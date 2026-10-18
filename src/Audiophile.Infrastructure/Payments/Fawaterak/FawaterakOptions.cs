namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Bound from the "Fawaterak" configuration section. The API key and vendor key come
// from the Fawaterak dashboard → Integration. Test accounts live on staging.fawaterk.com.
// Secrets go in user-secrets, never in appsettings.json.
public sealed class FawaterakOptions
{
    public const string SectionName = "Fawaterak";

    // Staging for testing; https://app.fawaterk.com/api/v2/ for live.
    public string BaseUrl { get; set; } = "https://staging.fawaterk.com/api/v2/";

    // Server only. Sent as "Authorization: Bearer …" on every API call.
    public string ApiKey { get; set; } = "";

    // Server only. HMAC-SHA256 key that proves a webhook came from Fawaterak.
    public string VendorKey { get; set; } = "";

    // Ids from GET getPaymentmethods. They differ between staging and live.
    public int CardPaymentMethodId { get; set; }

    // Public base URL of this API (the ngrok address), used to build webhook and return URLs.
    public string PublicBaseUrl { get; set; } = "";

    // Where FawaterakController.Return sends the customer's browser after paying:
    // the React client's orders page (Phase 4). It gets ?orderId=…&result=….
    public string ClientReturnUrl { get; set; } = "http://localhost:5173/orders";
}
