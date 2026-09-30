using System.Security.Cryptography;
using System.Text;

namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Fawaterak's webhook signature: HMAC-SHA256 over a query-string-shaped message,
// keyed with the vendor key, hex encoded. Two message shapes, per the docs:
// paid/failed use the invoice id, key and method; expired Fawry/Aman codes use
// the reference id and method.
public static class FawaterakHash
{
    public static string ForInvoice(string invoiceId, string invoiceKey, string paymentMethod, string vendorKey) =>
        Sign($"InvoiceId={invoiceId}&InvoiceKey={invoiceKey}&PaymentMethod={paymentMethod}", vendorKey);

    public static string ForExpiredReference(string referenceId, string paymentMethod, string vendorKey) =>
        Sign($"referenceId={referenceId}&PaymentMethod={paymentMethod}", vendorKey);

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
