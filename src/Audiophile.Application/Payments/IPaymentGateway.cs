using Audiophile.Domain.Enums;

namespace Audiophile.Application.Payments;

/// <summary>
/// One payment gateway, seen from the shop. Everything gateway-specific (URLs,
/// auth headers, field names, signature rules) lives behind this interface in
/// Infrastructure, so adding a second gateway is a new class, not a rewrite.
/// All amounts are in minor units (piasters).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Short stable name stored on each payment, e.g. "paymob".</summary>
    string Name { get; }

    /// <summary>Opens a hosted checkout session and returns where to send the customer.</summary>
    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct);

    Task<GatewayOperationResult> RefundAsync(string transactionId, long amountMinor, CancellationToken ct);

    Task<GatewayOperationResult> VoidAsync(string transactionId, CancellationToken ct);

    /// <summary>
    /// Asks the gateway what happened, instead of waiting to be told. Used when a
    /// webhook never arrived. Looks up by transaction id when known, otherwise by
    /// the gateway's order id.
    /// </summary>
    Task<IReadOnlyList<GatewayTransaction>> FindTransactionsAsync(
        string? transactionId, string? providerOrderId, CancellationToken ct);

    /// <summary>
    /// Checks a server-to-server notification's signature and reads it. Returns
    /// <see cref="GatewayCallback.Rejected"/> when the signature does not match.
    /// </summary>
    GatewayCallback ParseWebhook(string rawBody, IReadOnlyDictionary<string, string> query);

    /// <summary>Same check for the browser redirect after checkout. For display only.</summary>
    GatewayCallback ParseRedirect(IReadOnlyDictionary<string, string> query);
}

public sealed record CheckoutRequest(
    string MerchantReference,          // our order id; the gateway echoes it back
    long AmountMinor,
    string Currency,
    PaymentMethod Method,
    IReadOnlyList<CheckoutItem> Items,
    CheckoutCustomer Customer);

public sealed record CheckoutItem(string Name, long UnitAmountMinor, int Quantity);

public sealed record CheckoutCustomer(
    string FirstName, string LastName, string Email, string Phone,
    string Street, string City, string Country);

public sealed record CheckoutSession(string ProviderReference, string? ProviderOrderId, string CheckoutUrl);

public sealed record GatewayOperationResult(bool Success, string? TransactionId, string? Message);

/// <summary>A transaction as the gateway describes it, already translated to our terms.</summary>
public sealed record GatewayTransaction(
    string TransactionId,
    string? ProviderOrderId,
    string? MerchantReference,
    long AmountMinor,
    string Currency,
    bool Success,
    bool Pending,
    bool IsRefunded,
    bool IsVoided,
    bool IsChildTransaction,   // a refund / void / capture made against an earlier transaction
    string? SourceType);       // "card", "wallet", "aggregator" (kiosk)…

public sealed record GatewayCallback(bool IsAuthentic, string EventType, GatewayTransaction? Transaction)
{
    public static GatewayCallback Rejected { get; } = new(false, "rejected", null);
}
