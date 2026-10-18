using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

// The payment of one order, its amount always equals the order total and it moves from pending to succeeded or failed once
public sealed class Payment
{
    private Payment() { } // EF Core

    internal Payment(Guid orderId, PaymentMethod paymentMethod, decimal amount)
    {
        OrderId = orderId;
        PaymentMethod = paymentMethod;
        Amount = amount;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // The gateway that handles the payment, null for cash on delivery
    public string? Provider { get; private set; }

    // Fawaterak's invoice key
    public string? ProviderReference { get; private set; }

    // Fawaterak's invoice id
    public string? ProviderOrderId { get; private set; }

    // The id of the successful transaction
    public string? ProviderTransactionId { get; private set; }

    // The payment page the customer is sent to
    public string? CheckoutUrl { get; private set; }

    public Order Order { get; private set; } = null!;

    public bool UsesGateway => PaymentMethod != PaymentMethod.CashOnDelivery;

    public void LinkToProvider(string provider, string providerReference, string providerOrderId, string checkoutUrl)
    {
        EnsurePending();
        Provider = provider;
        ProviderReference = providerReference;
        ProviderOrderId = providerOrderId;
        CheckoutUrl = checkoutUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSucceeded(string transactionId)
    {
        EnsurePending();
        Status = PaymentStatus.Succeeded;
        ProviderTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        EnsurePending();
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"A {Status} payment cannot be changed.");
    }
}
