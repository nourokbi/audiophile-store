using Audiophile.Application.Abstractions;
using Audiophile.Application.Orders;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Payments;

public enum PaymentUpdateOutcome
{
    Applied,            // our state changed
    Duplicate,          // seen this exact event before; nothing to do
    AttemptFailed,      // one attempt was declined; the customer can still retry on the same checkout
    Ignored,            // a late event for a payment already closed
    Unmatched,          // no order of ours matches; stored for support to look at
    AmountMismatch,     // paid amount differs from the order total; NOT confirmed
    NeedsManualRefund   // money taken for an order we had already cancelled
}

// The single place a gateway's report about a transaction changes our state,
// whether the report came from a webhook or from us asking (inquiry). Both paths
// share the de-duplication key, so whichever arrives second is a no-op.
public sealed class PaymentResultApplier(IAppDbContext db, ILogger<PaymentResultApplier> logger)
{
    public async Task<PaymentUpdateOutcome> ApplyAsync(
        string provider, string source, GatewayTransaction tx, string rawPayload, CancellationToken ct)
    {
        // A kiosk transaction is reported twice with the same id: first "pending"
        // (reference issued), then "success" (cash paid). The state is part of the
        // key so the second report is not mistaken for a duplicate of the first.
        var state = tx.Pending ? "pending" : tx.Success ? "success" : "failed";
        var externalId = $"{tx.TransactionId}:{state}";

        if (await db.PaymentEvents.AnyAsync(e => e.Provider == provider && e.ExternalId == externalId, ct))
        {
            logger.LogInformation("Duplicate {Provider} event {ExternalId} ignored", provider, externalId);
            return PaymentUpdateOutcome.Duplicate;
        }

        var order = await FindOrderAsync(provider, tx, ct);
        db.PaymentEvents.Add(new PaymentEvent(provider, externalId, $"{source}:{state}", order?.Payment?.Id, rawPayload));

        var outcome = order is null
            ? PaymentUpdateOutcome.Unmatched
            : Apply(order, tx);

        // The event row and the order/payment/stock changes commit together. If two
        // deliveries of one event race past the AnyAsync check, the unique index on
        // (Provider, ExternalId) fails the second commit; the gateway retries it and
        // the retry is answered as a duplicate.
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "{Provider} {Source} event {ExternalId} for order {OrderId}: {Outcome}",
            provider, source, externalId, order?.Id, outcome);
        return outcome;
    }

    private PaymentUpdateOutcome Apply(Order order, GatewayTransaction tx)
    {
        var payment = order.Payment!;

        if (tx.Pending)
        {
            payment.RecordPendingTransaction(tx.TransactionId);
            return PaymentUpdateOutcome.Applied;
        }

        if (!tx.Success)
        {
            // Hosted checkout lets the customer try another card after a decline, so a
            // failed attempt does not fail the payment. The expiry job closes it if
            // nothing succeeds in time.
            logger.LogInformation("Payment attempt {TransactionId} for order {OrderId} was declined",
                tx.TransactionId, order.Id);
            return PaymentUpdateOutcome.AttemptFailed;
        }

        if (tx.AmountMinor != payment.AmountInMinorUnits)
        {
            logger.LogCritical(
                "Order {OrderId}: gateway reports {Paid} minor units paid but the order total is {Expected}. Not confirming.",
                order.Id, tx.AmountMinor, payment.AmountInMinorUnits);
            return PaymentUpdateOutcome.AmountMismatch;
        }

        if (payment.Status != PaymentStatus.Pending)
            return payment.ProviderTransactionId == tx.TransactionId
                ? PaymentUpdateOutcome.Duplicate
                : PaymentUpdateOutcome.Ignored;

        // A kiosk customer can pay after we expired the order. The money is real,
        // so record it, but the order stays cancelled and someone must refund.
        if (order.Status != OrderStatus.Pending)
        {
            payment.MarkSucceeded(tx.TransactionId);
            logger.LogError("Order {OrderId} is {Status} but payment {TransactionId} succeeded. Refund required.",
                order.Id, order.Status, tx.TransactionId);
            return PaymentUpdateOutcome.NeedsManualRefund;
        }

        payment.MarkSucceeded(tx.TransactionId);
        order.Confirm();
        return PaymentUpdateOutcome.Applied;
    }

    private async Task<Order?> FindOrderAsync(string provider, GatewayTransaction tx, CancellationToken ct)
    {
        var query = db.Orders
            .Include(o => o.Payment)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product);

        // Our own order id comes back as the merchant reference; prefer it.
        if (Guid.TryParse(tx.MerchantReference, out var orderId))
        {
            var byReference = await query.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (byReference?.Payment?.Provider == provider)
                return byReference;
        }

        return tx.ProviderOrderId is null
            ? null
            : await query.FirstOrDefaultAsync(
                o => o.Payment!.Provider == provider && o.Payment.ProviderOrderId == tx.ProviderOrderId, ct);
    }
}
