using Audiophile.Domain.Enums;

namespace Audiophile.Application.Payments;


public interface IPaymentGateway
{
    string Name { get; }

    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct);

    Task<GatewayOperationResult> RefundAsync(string transactionId, long amountMinor, CancellationToken ct);

    Task<GatewayOperationResult> VoidAsync(string transactionId, CancellationToken ct);

    Task<IReadOnlyList<GatewayTransaction>> FindTransactionsAsync(
        string? transactionId, string? providerOrderId, CancellationToken ct);

    Task<GatewayCallback> ParseWebhookAsync(
        string rawBody, IReadOnlyDictionary<string, string> query, CancellationToken ct);

    GatewayCallback ParseRedirect(IReadOnlyDictionary<string, string> query);
}

public interface IPaymentGatewayResolver
{
    IPaymentGateway Default { get; }

    IPaymentGateway Get(string name);
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

public sealed record CheckoutSession(
    string ProviderReference, string? ProviderOrderId, string CheckoutUrl, string? PaymentCode = null);

public sealed record GatewayOperationResult(bool Success, string? TransactionId, string? Message);

// A transaction as the gateway describes it, already translated to our terms.
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
