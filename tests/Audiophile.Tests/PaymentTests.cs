using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.ValueObjects;

namespace Audiophile.Tests;

public class PaymentTests
{
    [Fact]
    public void Partial_refunds_add_up_and_the_last_one_makes_it_refunded()
    {
        var payment = SucceededPayment(total: 1000m);

        payment.RecordRefund(300m);
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);

        payment.RecordRefund(200m);
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(500m, payment.RefundableAmount);

        payment.RecordRefund(500m);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(0m, payment.RefundableAmount);
    }

    [Fact]
    public void Cannot_refund_more_than_was_paid()
    {
        var payment = SucceededPayment(total: 1000m);
        payment.RecordRefund(900m);

        Assert.Throws<InvalidOperationException>(() => payment.RecordRefund(200m));
        Assert.Equal(900m, payment.RefundedAmount);
    }

    [Fact]
    public void Cannot_refund_a_payment_that_never_succeeded()
    {
        var payment = PendingPayment(total: 1000m);
        Assert.Throws<InvalidOperationException>(() => payment.RecordRefund(100m));
    }

    [Fact]
    public void Void_is_only_possible_before_any_refund()
    {
        var voidable = SucceededPayment(total: 1000m);
        voidable.MarkVoided();
        Assert.Equal(PaymentStatus.Voided, voidable.Status);

        var refunded = SucceededPayment(total: 1000m);
        refunded.RecordRefund(100m);
        Assert.Throws<InvalidOperationException>(refunded.MarkVoided);
    }

    [Fact]
    public void Succeeding_twice_is_a_no_op_not_an_error()
    {
        var payment = PendingPayment(total: 1000m);
        Assert.True(payment.MarkSucceeded("tx-1"));
        Assert.False(payment.MarkSucceeded("tx-1"));
    }

    [Fact]
    public void Amount_in_minor_units_is_piasters()
    {
        Assert.Equal(304_900, PendingPayment(total: 3049m).AmountInMinorUnits);
    }

    internal static Payment PendingPayment(decimal total)
    {
        var order = Order.Place(
            Guid.NewGuid(), PaymentMethod.Card,
            new ShippingDetails("Mona Hassan", "+201000000000", "12 Tahrir Street", "Cairo", "Egypt", null),
            [new OrderLine(Guid.NewGuid(), 1, total, "XX59 Headphones", null)],
            shippingFee: 0m, idempotencyKey: Guid.NewGuid().ToString());
        return order.StartPayment();
    }

    private static Payment SucceededPayment(decimal total)
    {
        var payment = PendingPayment(total);
        payment.MarkSucceeded("tx-1");
        return payment;
    }
}
