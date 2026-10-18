using System.Net;
using System.Security.Cryptography;
using System.Text;
using Audiophile.Application.Common;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Audiophile.Tests;

public class FawaterakGatewayTests
{
    private const string VendorKey = "test-vendor-key";
    private static readonly Guid OrderId = Guid.Parse("3f2b1c9e-5a44-4c1e-9d7a-0b6f2a9c1e11");

    [Fact]
    public async Task Paid_webhook_with_a_valid_hash_is_confirmed_with_the_invoice_lookup()
    {
        var lookup = new StubHandler("""
            {"status":"success","data":{"invoice_id":1000430,"invoice_key":"69zpnFIcIPYNBwG","paid":1,
             "total":3049.00,"currency":"EGP","payment_method":"Card","pay_load":{"order_id":"ORDER_ID"}}}
            """.Replace("ORDER_ID", OrderId.ToString()));

        var invoice = await CreateGateway(lookup).ReadPaidInvoiceAsync(PaidWebhook(ValidHash()), CancellationToken.None);

        Assert.NotNull(invoice);
        Assert.True(invoice.IsPaid);
        Assert.Equal("1000430", invoice.InvoiceId);
        Assert.Equal(3049m, invoice.Total);                // the amount comes from the lookup, not the webhook
        Assert.Equal(OrderId, invoice.OrderId);
        Assert.EndsWith("getInvoiceData/1000430", lookup.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", lookup.LastRequest.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Paid_webhook_with_a_wrong_hash_is_rejected_without_calling_fawaterak()
    {
        var lookup = new StubHandler("{}");

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateGateway(lookup).ReadPaidInvoiceAsync(PaidWebhook("forged"), CancellationToken.None));
        Assert.Null(lookup.LastRequest);
    }

    [Fact]
    public async Task Paid_webhook_is_not_trusted_when_the_lookup_says_unpaid()
    {
        var lookup = new StubHandler("""{"status":"success","data":{"invoice_id":1000430,"paid":0,"total":3049.00}}""");

        var invoice = await CreateGateway(lookup).ReadPaidInvoiceAsync(PaidWebhook(ValidHash()), CancellationToken.None);

        Assert.Null(invoice);
    }

    [Fact]
    public async Task Refund_notice_is_acknowledged_but_not_applied()
    {
        const string body = """
            {"transactionId":"2061562","amount":"100","currency":"EGP","status":"approved",
             "reason":"Product not available","approvedAt":"2026-02-22 14:57:40"}
            """;

        var invoice = await CreateGateway(new StubHandler("{}")).ReadPaidInvoiceAsync(body, CancellationToken.None);

        Assert.Null(invoice);
    }

    [Fact]
    public void Hash_follows_the_documented_message_format()
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(VendorKey));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(
            "InvoiceId=1000430&InvoiceKey=69zpnFIcIPYNBwG&PaymentMethod=Card"))).ToLowerInvariant();

        Assert.Equal(expected, FawaterakHash.ForInvoice("1000430", "69zpnFIcIPYNBwG", "Card", VendorKey));
    }

    private static string ValidHash() => FawaterakHash.ForInvoice("1000430", "69zpnFIcIPYNBwG", "Card", VendorKey);

    private static string PaidWebhook(string hash) => $$"""
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

    // Answers every request with the same JSON and remembers the last request
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
