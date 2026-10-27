using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;

namespace Audiophile.Tests;

public class OrderTests
{
    [Fact]
    public void Placing_an_order_takes_the_items_out_of_stock_and_sets_the_totals()
    {
        var headphones = Headphones(price: 1000m, stock: 5);

        var order = PlaceOrder(Guid.NewGuid(), (headphones, 2));

        Assert.Equal(3, headphones.Stock);
        Assert.Equal(2000m, order.SubTotal);
        Assert.Equal(50m, order.ShippingFee);
        Assert.Equal(2050m, order.TotalAmount);
        Assert.Equal(2050m, order.Payment!.Amount);
        Assert.Equal(PaymentStatus.Pending, order.Payment.Status);
    }

    [Fact]
    public void Ordering_more_than_is_in_stock_is_refused()
    {
        var headphones = Headphones(price: 1000m, stock: 1);

        Assert.Throws<InvalidOperationException>(() => PlaceOrder(Guid.NewGuid(), (headphones, 2)));
        Assert.Equal(1, headphones.Stock);
    }

    [Fact]
    public void Cancelling_returns_the_stock_and_fails_the_pending_payment()
    {
        var headphones = Headphones(price: 1000m, stock: 5);
        var order = PlaceOrder(Guid.NewGuid(), (headphones, 2));

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Failed, order.Payment!.Status);
        Assert.Equal(5, headphones.Stock);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_cancelled_again()
    {
        var headphones = Headphones(price: 1000m, stock: 5);
        var order = PlaceOrder(Guid.NewGuid(), (headphones, 2));
        order.Cancel();

        Assert.Throws<InvalidOperationException>(order.Cancel);
        Assert.Equal(5, headphones.Stock);
    }

    [Fact]
    public void A_card_order_is_only_confirmed_once_its_payment_succeeded()
    {
        var order = PlaceOrder(Guid.NewGuid(), (Headphones(price: 1000m, stock: 5), 1));

        Assert.Equal([OrderStatus.Cancelled], order.NextStatuses);
        Assert.Throws<InvalidOperationException>(order.Confirm);

        order.Payment!.MarkSucceeded("tx-1");
        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void A_payment_can_only_succeed_once()
    {
        var payment = PlaceOrder(Guid.NewGuid(), (Headphones(price: 1000m, stock: 5), 1)).Payment!;

        payment.MarkSucceeded("tx-1");

        Assert.Throws<InvalidOperationException>(() => payment.MarkSucceeded("tx-2"));
        Assert.Equal("tx-1", payment.ProviderTransactionId);
    }

    internal static Order PlaceOrder(Guid userId, params (Product Product, int Quantity)[] items)
    {
        var address = new Address(userId, "Mona Hassan", "12 Tahrir Street", "Cairo", "Egypt", "+201000000000");
        return Order.Place(userId, PaymentMethod.Card, address, items, idempotencyKey: Guid.NewGuid().ToString());
    }

    internal static Product Headphones(decimal price, int stock, string slug = "test-headphones") =>
        new("XX59 Headphones", "Enjoy your audio almost anywhere.", price, stock, ProductCategory.Headphones, slug: slug);
}
