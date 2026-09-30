using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

/// <summary>
/// One notification received from a payment gateway, stored exactly as it arrived.
/// Two jobs: a unique (Provider, ExternalId) index makes a redelivered webhook a
/// no-op, and the raw payload is the audit trail support reads when a customer
/// says "I paid but my order is still pending".
/// </summary>
public sealed class PaymentEvent
{
    private PaymentEvent() { } // EF Core

    public PaymentEvent(string provider, string externalId, string eventType, Guid? paymentId, string rawPayload)
    {
        Provider = Guard.Text(provider, 1, 50, nameof(provider));
        ExternalId = Guard.Text(externalId, 1, 255, nameof(externalId));
        EventType = Guard.Text(eventType, 1, 100, nameof(eventType));
        PaymentId = paymentId;
        RawPayload = rawPayload ?? string.Empty;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Provider { get; private set; } = null!;

    /// <summary>The gateway's id for this event (Paymob: the transaction id, <c>obj.id</c>).</summary>
    public string ExternalId { get; private set; } = null!;

    public string EventType { get; private set; } = null!;
    public Guid? PaymentId { get; private set; }
    public string RawPayload { get; private set; } = null!;
    public DateTime ReceivedAt { get; private set; } = DateTime.UtcNow;
}
