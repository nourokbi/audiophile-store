using System.Security.Cryptography;
using System.Text;

namespace Audiophile.Infrastructure.Payments.Fawaterak;

// Fawaterak's webhook signature, an HMAC SHA256 of the invoice id, invoice key and payment method keyed with the vendor key
public static class FawaterakHash
{
    public static string ForInvoice(string invoiceId, string invoiceKey, string paymentMethod, string vendorKey)
    {
        var message = $"InvoiceId={invoiceId}&InvoiceKey={invoiceKey}&PaymentMethod={paymentMethod}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(vendorKey), Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Compares in constant time so the comparison does not leak how many characters matched
    public static bool Matches(string expectedHash, string? receivedHash) =>
        !string.IsNullOrEmpty(receivedHash)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHash),
            Encoding.ASCII.GetBytes(receivedHash.Trim().ToLowerInvariant()));
}
