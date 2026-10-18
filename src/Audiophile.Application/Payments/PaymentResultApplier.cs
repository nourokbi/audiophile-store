using Audiophile.Application.Abstractions;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Payments;

public enum PaymentUpdateOutcome
{
    Applied,            // the payment succeeded and the order is confirmed
    NotPaidYet,         // the invoice is still open
    Duplicate,          // this result was already applied
    Unmatched,          // no order matches the invoice
    AmountMismatch,     // the paid amount differs from the order total, so nothing is confirmed
    NeedsManualRefund   // money arrived for an order that was already cancelled
}

// The one place a Fawaterak invoice result changes an order, used by both the webhook and the sync
public sealed class PaymentResultApplier(IAppDbContext db, ILogger<PaymentResultApplier> logger)
{
    public async Task<PaymentUpdateOutcome> ApplyAsync(
        string source, FawaterakInvoice invoice, string rawPayload, CancellationToken ct)
    {
        // The paid state is part of the key, so an unpaid check followed by a paid webhook are two different events
        var state = invoice.IsPaid ? "success" : "pending";
        var eventKey = $"{invoice.InvoiceId}:{state}";

        if (await db.PaymentEvents.AnyAsync(e => e.Provider == IFawaterakGateway.Provider && e.ExternalId == eventKey, ct))
        {
            logger.LogInformation("Duplicate Fawaterak event {EventKey} ignored", eventKey);
            return PaymentUpdateOutcome.Duplicate;
        }

        var order = await FindOrderAsync(invoice, ct);
        db.PaymentEvents.Add(new PaymentEvent(
            IFawaterakGateway.Provider, eventKey, $"{source}:{state}", order?.Payment?.Id, rawPayload));

        var outcome = order is null ? PaymentUpdateOutcome.Unmatched : ApplyToOrder(order, invoice);

        // A unique index on the event key stops a second delivery of the same event that arrives at the same moment
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Fawaterak {Source} event {EventKey} for order {OrderId}: {Outcome}",
            source, eventKey, order?.Id, outcome);
        return outcome;
    }

    private PaymentUpdateOutcome ApplyToOrder(Order order, FawaterakInvoice invoice)
    {
        var payment = order.Payment!;

        if (!invoice.IsPaid)
            return PaymentUpdateOutcome.NotPaidYet;

        if (invoice.Total != payment.Amount)
        {
            logger.LogCritical("Order {OrderId} was paid {Paid} but its total is {Expected}, so it is not confirmed",
                order.Id, invoice.Total, payment.Amount);
            return PaymentUpdateOutcome.AmountMismatch;
        }

        if (payment.Status == PaymentStatus.Succeeded)
            return PaymentUpdateOutcome.Duplicate;

        if (order.Status == OrderStatus.Cancelled)
        {
            logger.LogError("Order {OrderId} was cancelled but invoice {InvoiceId} was paid, refund it from the Fawaterak dashboard",
                order.Id, invoice.InvoiceId);
            return PaymentUpdateOutcome.NeedsManualRefund;
        }

        payment.MarkSucceeded(invoice.InvoiceId);
        order.Confirm();
        return PaymentUpdateOutcome.Applied;
    }

    // Looks the order up by our own id first and by Fawaterak's invoice id when ours is missing
    private async Task<Order?> FindOrderAsync(FawaterakInvoice invoice, CancellationToken ct)
    {
        var orders = db.Orders.Include(o => o.Payment);

        var order = invoice.OrderId is { } orderId
            ? await orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            : null;

        return order ?? await orders.FirstOrDefaultAsync(o => o.Payment!.ProviderOrderId == invoice.InvoiceId, ct);
    }
}
