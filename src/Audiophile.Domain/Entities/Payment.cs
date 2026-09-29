using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

/// <summary>
/// The payment attempt for an order. Created by <see cref="Order.StartPayment"/>
/// so <see cref="Amount"/> always equals the order's total.
/// </summary>
public sealed class Payment
{
    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> AllowedTransitions = new()
    {
        [PaymentStatus.Pending] = [PaymentStatus.Succeeded, PaymentStatus.Failed],
        [PaymentStatus.Succeeded] = [PaymentStatus.Refunded],
        [PaymentStatus.Failed] = [],
        [PaymentStatus.Refunded] = [],
    };

    private Payment() { } // EF Core

    internal Payment(Guid orderId, PaymentMethod paymentMethod, decimal amount)
    {
        OrderId = Guard.NotEmpty(orderId, nameof(orderId));
        PaymentMethod = Guard.Defined(paymentMethod, nameof(paymentMethod));
        Amount = Guard.PositiveMoney(amount, nameof(amount));
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Order Order { get; private set; } = null!;

    public bool IsSettled => Status is PaymentStatus.Succeeded or PaymentStatus.Refunded;

    /// <summary>M4-09 — records the PaymentIntent whose webhook will later settle this payment.</summary>
    public void LinkToStripeIntent(string paymentIntentId)
    {
        if (PaymentMethod != PaymentMethod.Card)
            throw new InvalidOperationException($"A {PaymentMethod} payment does not go through Stripe.");

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"A {Status} payment cannot be linked to a PaymentIntent.");

        StripePaymentIntentId = Guard.Text(paymentIntentId, 1, 255, nameof(paymentIntentId));
    }

    /// <summary>
    /// M4-11 — returns <c>false</c> when the payment had already succeeded, so a
    /// webhook delivered twice is told "nothing to do" rather than throwing.
    /// </summary>
    public bool MarkSucceeded() => TryTransitionTo(PaymentStatus.Succeeded);

    public bool MarkFailed() => TryTransitionTo(PaymentStatus.Failed);

    public bool Refund() => TryTransitionTo(PaymentStatus.Refunded);

    private bool TryTransitionTo(PaymentStatus next)
    {
        if (Status == next)
            return false;

        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidOperationException($"A payment that is {Status} cannot become {next}.");

        Status = next;
        return true;
    }
}
