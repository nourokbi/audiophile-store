using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

// One gateway notification stored as it arrived, so a repeated webhook is ignored and support can read what happened
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

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Provider { get; private set; } = null!;

    // The invoice id plus its paid state, like 3072989001:success
    public string ExternalId { get; private set; } = null!;

    public string EventType { get; private set; } = null!;
    public Guid? PaymentId { get; private set; }
    public string RawPayload { get; private set; } = null!;
    public DateTime ReceivedAt { get; private set; } = DateTime.UtcNow;
}
