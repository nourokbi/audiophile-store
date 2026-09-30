using System.Net;
using System.Text;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Audiophile.Tests;

public class FawaterakGatewayTests
{
    private const string VendorKey = "test-vendor-key";
    private const string OrderId = "3f2b1c9e-5a44-4c1e-9d7a-0b6f2a9c1e11";

    [Fact]
    public async Task Paid_webhook_with_valid_hash_is_confirmed_using_the_invoice_lookup()
    {
        var lookup = new StubHandler("""
            {"status":"success","data":{"invoice_id":1000430,"invoice_key":"69zpnFIcIPYNBwG","paid":1,
             "total":3049.00,"currency":"EGP","payment_method":"Card","pay_load":{"order_id":"ORDER_ID"}}}
            """.Replace("ORDER_ID", OrderId));
        var gateway = CreateGateway(lookup);

        var callback = await gateway.ParseWebhookAsync(PaidBody(ValidPaidHash()), CancellationToken.None);

        Assert.True(callback.IsAuthentic);
        var tx = callback.Transaction!;
        Assert.True(tx.Success);
        Assert.Equal("1000430", tx.TransactionId);
        Assert.Equal(304_900, tx.AmountMinor);             // amount comes from the lookup, not the webhook
        Assert.Equal(OrderId, tx.MerchantReference);
        Assert.EndsWith("getInvoiceData/1000430", lookup.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", lookup.LastRequest.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Paid_webhook_with_wrong_hash_is_rejected_without_calling_fawaterak()
    {
        var lookup = new StubHandler("{}");
        var callback = await CreateGateway(lookup).ParseWebhookAsync(PaidBody("forged"), CancellationToken.None);

        Assert.False(callback.IsAuthentic);
        Assert.Null(lookup.LastRequest);
    }

    [Fact]
    public async Task Paid_webhook_is_not_trusted_when_the_lookup_says_unpaid()
    {
        var lookup = new StubHandler("""{"status":"success","data":{"invoice_id":1000430,"paid":0,"total":3049.00}}""");

        var callback = await CreateGateway(lookup).ParseWebhookAsync(PaidBody(ValidPaidHash()), CancellationToken.None);

        Assert.True(callback.IsAuthentic);
        Assert.Null(callback.Transaction);                 // acknowledged, but nothing is applied
    }

    [Fact]
    public async Task Expired_aman_code_with_valid_hash_is_reported_as_not_paid()
    {
        var hash = FawaterakHash.ForExpiredReference("778586510", "Aman", VendorKey);
        var body = $$"""
            {"hashKey":"{{hash}}","referenceId":"778586510","status":"EXPIRED","paymentMethod":"Aman",
             "pay_load":"{\"order_id\":\"{{OrderId}}\"}","transactionId":28180,"transactionKey":"Asbv2zmnFfdUOOe"}
            """;

        var callback = await CreateGateway(new StubHandler("{}")).ParseWebhookAsync(body, CancellationToken.None);

        Assert.True(callback.IsAuthentic);
        Assert.False(callback.Transaction!.Success);
        Assert.False(callback.Transaction.Pending);
        Assert.Equal(OrderId, callback.Transaction.MerchantReference);   // pay_load arrived as a JSON string
    }

    [Fact]
    public async Task Unsigned_refund_notice_is_acknowledged_but_not_applied()
    {
        const string body = """
            {"transactionId":"2061562","amount":"100","currency":"EGP","status":"approved",
             "reason":"Product not available","approvedAt":"2026-02-22 14:57:40"}
            """;

        var callback = await CreateGateway(new StubHandler("{}")).ParseWebhookAsync(body, CancellationToken.None);

        Assert.True(callback.IsAuthentic);
        Assert.Null(callback.Transaction);
    }

    [Fact]
    public void Hash_follows_the_documented_message_format()
    {
        // Independent computation of HMAC-SHA256("InvoiceId=…&InvoiceKey=…&PaymentMethod=…", vendorKey).
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(VendorKey));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(
            "InvoiceId=1000430&InvoiceKey=69zpnFIcIPYNBwG&PaymentMethod=Card"))).ToLowerInvariant();

        Assert.Equal(expected, FawaterakHash.ForInvoice("1000430", "69zpnFIcIPYNBwG", "Card", VendorKey));
    }

    // ------------------------------------------------------------- helpers

    private static string ValidPaidHash() => FawaterakHash.ForInvoice("1000430", "69zpnFIcIPYNBwG", "Card", VendorKey);

    private static string PaidBody(string hash) => $$"""
        {"hashKey":"{{hash}}","invoice_key":"69zpnFIcIPYNBwG","invoice_id":1000430,
         "payment_method":"Card","invoice_status":"paid","pay_load":null,"referenceNumber":"982443480"}
        """;

    private static FawaterakGateway CreateGateway(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://staging.fawaterk.com/api/v2/") },
        Options.Create(new FawaterakOptions
        {
            ApiKey = "test-api-key", VendorKey = VendorKey, PublicBaseUrl = "https://example.test",
        }),
        NullLogger<FawaterakGateway>.Instance);

    private sealed class StubHandler(string responseJson) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }
}
