using Audiophile.Domain.Enums;

namespace Audiophile.Application.Payments;

// Fawaterak is the only payment gateway. This interface lives in Application so
// OrderService can call it without knowing about HTTP; FawaterakGateway in
// Infrastructure implements it (F12 / Ctrl+F12 jumps straight there).
public interface IFawaterakGateway
{
    public const string Provider = "fawaterak";

    // POST invoiceInitPay → the hosted payment page the customer is sent to.
    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct);

    // GET getInvoiceData/{invoiceId} → Fawaterak's own view of the invoice (paid or not, how much).
    Task<GatewayTransaction> GetInvoiceAsync(string invoiceId, CancellationToken ct);

    // Verifies the webhook's hashKey and translates the body into our terms.
    Task<GatewayCallback> ParseWebhookAsync(string rawBody, CancellationToken ct);
}

public sealed record CheckoutRequest(
    string MerchantReference,          // our order id; Fawaterak echoes it back in pay_load
    long AmountMinor,
    string Currency,
    PaymentMethod Method,
    IReadOnlyList<CheckoutItem> Items,
    CheckoutCustomer Customer);

public sealed record CheckoutItem(string Name, long UnitAmountMinor, int Quantity);

public sealed record CheckoutCustomer(
    string FirstName, string LastName, string Email, string Phone,
    string Street, string City, string Country);

// ProviderReference = invoice_key, ProviderOrderId = invoice_id, PaymentCode = Aman/Fawry code.
public sealed record CheckoutSession(
    string ProviderReference, string? ProviderOrderId, string CheckoutUrl, string? PaymentCode = null);

// A Fawaterak invoice or webhook, already translated to our terms.
public sealed record GatewayTransaction(
    string TransactionId,
    string? ProviderOrderId,
    string? MerchantReference,
    long AmountMinor,
    string Currency,
    bool Success,
    bool Pending,
    string? SourceType);       // "Card", "Fawry", "Aman"…

public sealed record GatewayCallback(bool IsAuthentic, string EventType, GatewayTransaction? Transaction)
{
    public static GatewayCallback Rejected { get; } = new(false, "rejected", null);
}
