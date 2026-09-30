using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.ValueObjects;
using Audiophile.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Audiophile.Tests;

// The webhook's business rules, run against a real (in-memory SQLite) database
// so the unique index that blocks duplicate events is part of what is tested.
public sealed class PaymentResultApplierTests : IDisposable
{
    private const string Provider = "fawaterak";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public PaymentResultApplierTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Successful_payment_confirms_the_order()
    {
        var (db, orderId) = await SeedPendingOrderAsync(PaymentMethod.Card, price: 1000m);

        var outcome = await Apply(db, Tx(orderId, "5001", amountMinor: 100_000, success: true));

        Assert.Equal(PaymentUpdateOutcome.Applied, outcome);
        var order = await Reload(orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(PaymentStatus.Succeeded, order.Payment!.Status);
        Assert.Equal("5001", order.Payment.ProviderTransactionId);
    }

    [Fact]
    public async Task Same_webhook_delivered_twice_confirms_once()
    {
        var (db, orderId) = await SeedPendingOrderAsync(PaymentMethod.Card, price: 1000m);
        var tx = Tx(orderId, "5001", amountMinor: 100_000, success: true);

        Assert.Equal(PaymentUpdateOutcome.Applied, await Apply(db, tx));
        Assert.Equal(PaymentUpdateOutcome.Duplicate, await Apply(NewContext(), tx));

        await using var check = NewContext();
        Assert.Equal(1, await check.PaymentEvents.CountAsync());
    }

    [Fact]
    public async Task Wrong_amount_is_never_confirmed()
    {
        var (db, orderId) = await SeedPendingOrderAsync(PaymentMethod.Card, price: 1000m);

        var outcome = await Apply(db, Tx(orderId, "5001", amountMinor: 100, success: true));

        Assert.Equal(PaymentUpdateOutcome.AmountMismatch, outcome);
        Assert.Equal(OrderStatus.Pending, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Declined_attempt_leaves_the_order_open_for_another_try()
    {
        var (db, orderId) = await SeedPendingOrderAsync(PaymentMethod.Card, price: 1000m);

        Assert.Equal(PaymentUpdateOutcome.AttemptFailed,
            await Apply(db, Tx(orderId, "5001", amountMinor: 100_000, success: false)));
        Assert.Equal(PaymentUpdateOutcome.Applied,
            await Apply(NewContext(), Tx(orderId, "5002", amountMinor: 100_000, success: true)));

        Assert.Equal(OrderStatus.Confirmed, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Kiosk_reference_first_then_cash_payment_confirms()
    {
        var (db, orderId) = await SeedPendingOrderAsync(PaymentMethod.Kiosk, price: 900m);

        // Same transaction id twice: first "reference issued", then "paid".
        Assert.Equal(PaymentUpdateOutcome.Applied,
            await Apply(db, Tx(orderId, "7001", amountMinor: 90_000, success: false, pending: true)));
        Assert.Equal(OrderStatus.Pending, (await Reload(orderId)).Status);

        Assert.Equal(PaymentUpdateOutcome.Applied,
            await Apply(NewContext(), Tx(orderId, "7001", amountMinor: 90_000, success: true)));
        Assert.Equal(OrderStatus.Confirmed, (await Reload(orderId)).Status);
    }

    [Fact]
    public async Task Event_for_an_unknown_order_is_stored_but_changes_nothing()
    {
        var (db, _) = await SeedPendingOrderAsync(PaymentMethod.Card, price: 1000m);

        var outcome = await Apply(db, Tx(Guid.NewGuid(), "9999", amountMinor: 100_000, success: true, providerOrderId: "none"));

        Assert.Equal(PaymentUpdateOutcome.Unmatched, outcome);
        await using var check = NewContext();
        Assert.Equal(1, await check.PaymentEvents.CountAsync(e => e.PaymentId == null));
    }

    // ------------------------------------------------------------- helpers

    private AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private async Task<(AppDbContext Db, Guid OrderId)> SeedPendingOrderAsync(PaymentMethod method, decimal price)
    {
        var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        var user = User.Create("Mona Hassan", $"{Guid.NewGuid():N}@example.com");
        var product = new Product("XX59 Headphones", "Enjoy your audio almost anywhere.", price, 10, ProductCategory.Headphones);
        product.DecreaseStock(1);

        var order = Order.Place(user.Id, method,
            new ShippingDetails("Mona Hassan", "+201000000000", "12 Tahrir Street", "Cairo", "Egypt", null),
            [OrderLine.FromProduct(product, 1)],
            shippingFee: 0m, idempotencyKey: Guid.NewGuid().ToString());
        order.StartPayment().LinkToProvider(Provider, "pi_test_1", $"po-{order.Id:N}", "https://checkout.example/1");

        db.AddRange(user, product, order);
        await db.SaveChangesAsync();
        return (NewContext(), order.Id);
    }

    private static async Task<PaymentUpdateOutcome> Apply(AppDbContext db, GatewayTransaction tx)
    {
        await using (db)
            return await new PaymentResultApplier(db, NullLogger<PaymentResultApplier>.Instance)
                .ApplyAsync(Provider, "test", tx, "{}", CancellationToken.None);
    }

    private async Task<Order> Reload(Guid orderId)
    {
        await using var db = NewContext();
        return await db.Orders.Include(o => o.Payment).SingleAsync(o => o.Id == orderId);
    }

    private static GatewayTransaction Tx(
        Guid orderId, string id, long amountMinor, bool success, bool pending = false, string? providerOrderId = null) =>
        new(id, providerOrderId, orderId.ToString(), amountMinor, "EGP",
            success, pending, IsRefunded: false, IsVoided: false, IsChildTransaction: false, SourceType: "card");
}
