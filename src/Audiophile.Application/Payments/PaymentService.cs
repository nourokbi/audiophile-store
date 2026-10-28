using System.Text.Json;
using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Application.Orders;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Payments;

// Everything about an order's card payment: opening the Fawaterak page, checking an invoice, and closing payments nobody finished
public sealed class PaymentService(
    IAppDbContext db,
    IFawaterakGateway fawaterak,
    PaymentResultApplier applier,
    ILogger<PaymentService> logger)
{
    // How long a customer has to pay on Fawaterak's page before the order is cancelled
    public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(30);

    // Creates a Fawaterak invoice for the order and keeps its payment page link on the payment
    public async Task OpenCheckoutAsync(Order order, CancellationToken ct)
    {
        var customerEmail = await db.Users.Where(u => u.Id == order.UserId).Select(u => u.Email!).FirstAsync(ct);
        var checkout = await fawaterak.CreateInvoiceAsync(order, customerEmail, ct);
        order.Payment!.LinkToProvider(IFawaterakGateway.Provider, checkout.InvoiceKey, checkout.InvoiceId, checkout.PaymentUrl);
    }

    // A failed attempt closes the Fawaterak invoice, so paying again opens a new one for the same order
    public async Task<OrderDto> RetryPaymentAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        if (order.Status != OrderStatus.Pending || order.Payment is not { UsesGateway: true, Status: PaymentStatus.Pending })
            throw new ConflictException("This order is not waiting for a payment.");
        if (order.Payment.CreatedAt < DateTime.UtcNow - PaymentWindow)
            throw new ConflictException("The time to pay this order has run out, its items go back into stock.");

        // The last attempt may have gone through after all, then the order is confirmed instead of charged twice
        if (order.Payment.ProviderOrderId is not null)
        {
            var synced = await SyncWithGatewayAsync(orderId, ct);
            if (synced.Payment?.Status != PaymentStatus.Pending)
                return synced;
        }

        await OpenCheckoutAsync(order, ct);
        await db.SaveChangesAsync(ct);
        return OrderDto.From(order);
    }

    // Asks Fawaterak for the real invoice status, for when a webhook never arrived
    public async Task<OrderDto> SyncWithGatewayAsync(Guid orderId, CancellationToken ct)
    {
        var invoiceId = await db.Payments
            .Where(p => p.OrderId == orderId)
            .Select(p => p.ProviderOrderId)
            .FirstOrDefaultAsync(ct)
            ?? throw new ConflictException($"Order {orderId} has no Fawaterak invoice.");

        var invoice = await fawaterak.GetInvoiceAsync(invoiceId, ct);
        await applier.ApplyAsync("inquiry", invoice, JsonSerializer.Serialize(invoice), ct);

        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.Id == orderId, ct);
        return OrderDto.From(order);
    }

    // Cancels card orders still unpaid after the payment window, asking Fawaterak first so a paid order is never cancelled
    public async Task<int> ExpireUnpaidOrdersAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow - PaymentWindow;
        var unpaidOrderIds = await db.Payments
            .Where(p => p.Status == PaymentStatus.Pending
                        && p.PaymentMethod != PaymentMethod.CashOnDelivery
                        && p.CreatedAt < deadline)
            .Select(p => p.OrderId)
            .ToListAsync(ct);

        var expiredCount = 0;
        foreach (var orderId in unpaidOrderIds)
        {
            try
            {
                var synced = await SyncWithGatewayAsync(orderId, ct);
                if (synced.Payment?.Status != PaymentStatus.Pending)
                    continue;
            }
            catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
            {
                logger.LogWarning(ex, "Could not check order {OrderId} with Fawaterak, will try again later", orderId);
                continue;
            }
            catch (ConflictException)
            {
                // The order never reached Fawaterak, so there is nothing to ask and it can expire
            }

            var order = await db.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstAsync(o => o.Id == orderId, ct);
            order.Cancel();
            await db.SaveChangesAsync(ct);
            expiredCount++;
            logger.LogInformation("Order {OrderId} expired unpaid and its stock was returned", orderId);
        }

        return expiredCount;
    }

    // Every Fawaterak notification stored for the order, oldest first, for support to read
    public async Task<IReadOnlyList<PaymentEventDto>> GetPaymentEventsAsync(Guid orderId, CancellationToken ct)
    {
        var paymentId = await db.Payments
            .Where(p => p.OrderId == orderId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Order {orderId} has no payment.");

        return await db.PaymentEvents.AsNoTracking()
            .Where(e => e.PaymentId == paymentId)
            .OrderBy(e => e.ReceivedAt)
            .Select(e => new PaymentEventDto(e.ReceivedAt, e.Provider, e.EventType, e.ExternalId, e.RawPayload))
            .ToListAsync(ct);
    }
}

public sealed record PaymentEventDto(
    DateTime ReceivedAt, string Provider, string EventType, string ExternalId, string RawPayload);
