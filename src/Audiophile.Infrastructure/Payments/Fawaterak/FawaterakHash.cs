using System.Security.Cryptography;
using System.Text;

namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Fawaterak's webhook signature: HMAC-SHA256 over a query-string-shaped message
// (invoice id, invoice key, payment method), keyed with the vendor key, hex encoded.
public static class FawaterakHash
{
    public static string ForInvoice(string invoiceId, string invoiceKey, string paymentMethod, string vendorKey) =>
        Sign($"InvoiceId={invoiceId}&InvoiceKey={invoiceKey}&PaymentMethod={paymentMethod}", vendorKey);

    public static string Sign(string message, string key)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Constant-time comparison.
    public static bool Matches(string expectedHex, string? receivedHex) =>
        !string.IsNullOrEmpty(receivedHex)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHex),
            Encoding.ASCII.GetBytes(receivedHex.Trim().ToLowerInvariant()));
}
