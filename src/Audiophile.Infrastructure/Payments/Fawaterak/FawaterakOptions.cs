namespace Audiophile.Infrastructure.Payments.Fawaterak;

/// <summary>
/// Bound from the "Fawaterak" configuration section. The API key and vendor key come
/// from the Fawaterak dashboard → Integration. Test accounts live on staging.fawaterk.com.
/// Secrets go in user-secrets, never in appsettings.json.
/// </summary>
public sealed class FawaterakOptions
{
    public const string SectionName = "Fawaterak";

    /// <summary>Staging for testing; https://app.fawaterk.com/api/v2/ for live.</summary>
    public string BaseUrl { get; set; } = "https://staging.fawaterk.com/api/v2/";

    /// <summary>Server only. Sent as "Authorization: Bearer …" on every API call.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Server only. HMAC-SHA256 key that proves a webhook came from Fawaterak.</summary>
    public string VendorKey { get; set; } = "";

    /// <summary>Ids from GET getPaymentmethods. They differ between staging and live.</summary>
    public int CardPaymentMethodId { get; set; }
    public int WalletPaymentMethodId { get; set; }
    public int KioskPaymentMethodId { get; set; }   // Aman

    /// <summary>Public base URL of this API (the ngrok address), used to build webhook and return URLs.</summary>
    public string PublicBaseUrl { get; set; } = "";
}
