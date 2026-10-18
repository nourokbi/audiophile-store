using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Audiophile.Tests;

// The webhook's business rules against a real database, so the unique index that blocks duplicate events is tested too
public sealed class PaymentResultApplierTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Successful_payment_confirms_the_order()
    {
        var (orderId, total) = await SeedPendingOrderAsync(price: 1000m);

        var outcome = await Apply(Invoice(orderId, "5001", total, isPaid: true));

        Assert.Equal(PaymentUpdateOutcome.Applied, outcome);
        var order = await Reload(orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(PaymentStatus.Succeeded, order.Payment!.Status);
        Assert.Equal("5001", order.Payment.ProviderTransactionId);
    }

    [Fact]
    public async Task Same_webhook_delivered_twice_confirms_once()
    {
        var (orderId, total) = await SeedPendingOrderAsync(price: 1000m);
        var invoice = Invoice(orderId, "5001", total, isPaid: true);

        Assert.Equal(PaymentUpdateOutcome.Applied, await Apply(invoice));
        Assert.Equal(PaymentUpdateOutcome.Duplicate, await Apply(invoice));

        await using var db = _database.NewContext();
        Assert.Equal(1, await db.PaymentEvents.CountAsync());
    }

    [Fact]
    public async Task Wrong_amount_is_never_confirmed()
    {
        var (orderId, total) = await SeedPendingOrderAsync(price: 1000m);

        var outcome = await Apply(Invoice(orderId, "5001", total - 1m, isPaid: true));

        Assert.Equal(PaymentUpdateOutcome.AmountMismatch, outcome);
        Assert.Equal(OrderStatus.Pending, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Unpaid_invoice_first_then_paid_confirms()
    {
        var (orderId, total) = await SeedPendingOrderAsync(price: 900m);

        // A sync while the customer is still on the payment page, then the paid webhook for the same invoice
        Assert.Equal(PaymentUpdateOutcome.NotPaidYet, await Apply(Invoice(orderId, "7001", total, isPaid: false)));
        Assert.Equal(OrderStatus.Pending, (await Reload(orderId)).Status);

        Assert.Equal(PaymentUpdateOutcome.Applied, await Apply(Invoice(orderId, "7001", total, isPaid: true)));
        Assert.Equal(OrderStatus.Confirmed, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Payment_for_a_cancelled_order_needs_a_manual_refund()
    {
        var (orderId, total) = await SeedPendingOrderAsync(price: 1000m, cancelled: true);

        var outcome = await Apply(Invoice(orderId, "5001", total, isPaid: true));

        Assert.Equal(PaymentUpdateOutcome.NeedsManualRefund, outcome);
        Assert.Equal(OrderStatus.Cancelled, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Event_for_an_unknown_order_is_stored_but_changes_nothing()
    {
        await SeedPendingOrderAsync(price: 1000m);

        var outcome = await Apply(Invoice(Guid.NewGuid(), "9999", 1000m, isPaid: true));

        Assert.Equal(PaymentUpdateOutcome.Unmatched, outcome);
        await using var db = _database.NewContext();
        Assert.Equal(1, await db.PaymentEvents.CountAsync(e => e.PaymentId == null));
    }

    private async Task<(Guid OrderId, decimal Total)> SeedPendingOrderAsync(decimal price, bool cancelled = false)
    {
        await using var db = _database.NewContext();

        var user = User.Create("Mona Hassan", $"{Guid.NewGuid():N}@example.com");
        var order = OrderTests.PlaceOrder(user.Id, (OrderTests.Headphones(price, stock: 10), 1));
        order.Payment!.LinkToProvider(IFawaterakGateway.Provider, "invoice-key", $"invoice-{order.Id:N}", "https://checkout.example/1");
        if (cancelled)
            order.Cancel();

        db.AddRange(user, order);
        await db.SaveChangesAsync();
        return (order.Id, order.TotalAmount);
    }

    private async Task<PaymentUpdateOutcome> Apply(FawaterakInvoice invoice)
    {
        await using var db = _database.NewContext();
        return await new PaymentResultApplier(db, NullLogger<PaymentResultApplier>.Instance)
            .ApplyAsync("test", invoice, "{}", CancellationToken.None);
    }

    private async Task<Order> Reload(Guid orderId)
    {
        await using var db = _database.NewContext();
        return await db.Orders.Include(o => o.Payment).SingleAsync(o => o.Id == orderId);
    }

    private static FawaterakInvoice Invoice(Guid orderId, string invoiceId, decimal total, bool isPaid) =>
        new(invoiceId, orderId, total, isPaid);
}
