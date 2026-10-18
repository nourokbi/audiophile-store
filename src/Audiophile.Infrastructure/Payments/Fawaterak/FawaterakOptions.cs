namespace Audiophile.Infrastructure.Payments.Fawaterak;

// The Fawaterak settings, the API key and vendor key are secrets kept in user secrets
public sealed class FawaterakOptions
{
    public const string SectionName = "Fawaterak";

    // Staging for testing and app.fawaterk.com for live payments
    public string BaseUrl { get; set; } = "https://staging.fawaterk.com/api/v2/";

    // Sent as a bearer token on every API call
    public string ApiKey { get; set; } = "";

    // The key that proves a webhook came from Fawaterak
    public string VendorKey { get; set; } = "";

    // The card method id from GET getPaymentmethods, it differs between staging and live
    public int CardPaymentMethodId { get; set; }

    // The public address of this API, the ngrok address locally, used for the webhook and return links
    public string PublicBaseUrl { get; set; } = "";

    // The shop page the customer's browser is sent to after paying
    public string ClientReturnUrl { get; set; } = "http://localhost:5173/orders";
}
