using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

/// <summary>
/// The payment for an order. Created by <see cref="Order.StartPayment"/> so
/// <see cref="Amount"/> always equals the order's total. Knows nothing about
/// any one gateway: the provider's identifiers are stored as opaque strings.
/// </summary>
public sealed class Payment
{
    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> AllowedTransitions = new()
    {
        [PaymentStatus.Pending] = [PaymentStatus.Succeeded, PaymentStatus.Failed],
        [PaymentStatus.Succeeded] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded, PaymentStatus.Voided],
        [PaymentStatus.PartiallyRefunded] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
        [PaymentStatus.Failed] = [],
        [PaymentStatus.Refunded] = [],
        [PaymentStatus.Voided] = [],
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
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Which gateway handled it, e.g. "paymob". Null for cash on delivery.</summary>
    public string? Provider { get; private set; }

    /// <summary>The gateway's id for the checkout session (Paymob: the intention id).</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>The gateway's order id (Paymob: <c>intention_order_id</c> / <c>order.id</c>).</summary>
    public string? ProviderOrderId { get; private set; }

    /// <summary>The gateway's transaction id. Refunds and voids are issued against this.</summary>
    public string? ProviderTransactionId { get; private set; }

    /// <summary>The hosted checkout page. Kept so a retried order request can return the same link.</summary>
    public string? CheckoutUrl { get; private set; }

    public Order Order { get; private set; } = null!;

    public bool UsesGateway => PaymentMethod != PaymentMethod.CashOnDelivery;
    public bool IsSettled => Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded;
    public decimal RefundableAmount => IsSettled ? Amount - RefundedAmount : 0m;

    /// <summary>Amount in the smallest currency unit (piasters), which is what gateways expect.</summary>
    public long AmountInMinorUnits => (long)(Amount * 100);

    /// <summary>Records the checkout session the customer is sent to.</summary>
    public void LinkToProvider(string provider, string providerReference, string? providerOrderId, string checkoutUrl)
    {
        if (!UsesGateway)
            throw new InvalidOperationException($"A {PaymentMethod} payment does not go through a gateway.");

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"A {Status} payment cannot be linked to a gateway session.");

        Provider = Guard.Text(provider, 1, 50, nameof(provider));
        ProviderReference = Guard.Text(providerReference, 1, 255, nameof(providerReference));
        ProviderOrderId = Guard.OptionalText(providerOrderId, 255, nameof(providerOrderId));
        CheckoutUrl = Guard.Text(checkoutUrl, 1, 2048, nameof(checkoutUrl));
        Touch();
    }

    /// <summary>
    /// The gateway reported a transaction that is still open, e.g. a kiosk
    /// reference was issued and the customer has not paid yet. Status stays Pending.
    /// </summary>
    public void RecordPendingTransaction(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            return;

        ProviderTransactionId = Guard.Text(transactionId, 1, 255, nameof(transactionId));
        Touch();
    }

    /// <summary>
    /// Returns <c>false</c> when the payment had already succeeded, so a webhook
    /// delivered twice is told "nothing to do" rather than throwing.
    /// </summary>
    public bool MarkSucceeded(string transactionId)
    {
        if (!TryTransitionTo(PaymentStatus.Succeeded))
            return false;

        ProviderTransactionId = Guard.Text(transactionId, 1, 255, nameof(transactionId));
        return true;
    }

    public bool MarkSucceeded() => TryTransitionTo(PaymentStatus.Succeeded);

    public bool MarkFailed(string? transactionId = null)
    {
        if (!TryTransitionTo(PaymentStatus.Failed))
            return false;

        if (transactionId is not null)
            ProviderTransactionId = Guard.Text(transactionId, 1, 255, nameof(transactionId));
        return true;
    }

    /// <summary>Full or partial. Total refunds can never exceed what was taken.</summary>
    public void RecordRefund(decimal amount)
    {
        Guard.PositiveMoney(amount, nameof(amount));

        if (amount > RefundableAmount)
            throw new InvalidOperationException(
                $"Cannot refund {amount}; only {RefundableAmount} of this payment is refundable.");

        var refundedAfter = RefundedAmount + amount;
        TryTransitionTo(refundedAfter == Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded);
        RefundedAmount = refundedAfter;
    }

    /// <summary>Cancels a successful payment before it settles. Only valid with no refunds yet.</summary>
    public void MarkVoided() => TryTransitionTo(PaymentStatus.Voided);

    private bool TryTransitionTo(PaymentStatus next)
    {
        // PartiallyRefunded → PartiallyRefunded is a real second refund, not a duplicate.
        if (Status == next && next != PaymentStatus.PartiallyRefunded)
            return false;

        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidOperationException($"A payment that is {Status} cannot become {next}.");

        Status = next;
        Touch();
        return true;
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
