using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Tests.Api;

// Paying a card order again after a failed attempt, with a fake Fawaterak that numbers its invoices
public sealed class PaymentRetryApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task Paying_again_opens_a_new_payment_page_for_the_same_order()
    {
        using var baseFactory = new ApiFactory();
        var gateway = new FakeGateway();
        using var factory = WithGateway(baseFactory, gateway);
        var customer = await SignUpAsync(factory, "mona@example.com");
        var order = await PlaceOrderAsync(customer, factory, "Card");
        var orderId = order.GetProperty("id").GetString();

        var response = await customer.PostAsync($"/orders/{orderId}/payment/retry", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var retried = await ReadAsync(response);
        Assert.Equal(orderId, retried.GetProperty("id").GetString());
        Assert.Equal("Pending", retried.GetProperty("status").GetString());
        Assert.Equal("https://pay.example.com/invoice-1", order.GetProperty("payment").GetProperty("checkoutUrl").GetString());
        Assert.Equal("https://pay.example.com/invoice-2", retried.GetProperty("payment").GetProperty("checkoutUrl").GetString());
    }

    [Fact]
    public async Task Paying_again_confirms_the_order_when_the_last_attempt_went_through()
    {
        using var baseFactory = new ApiFactory();
        var gateway = new FakeGateway();
        using var factory = WithGateway(baseFactory, gateway);
        var customer = await SignUpAsync(factory, "mona@example.com");
        var orderId = (await PlaceOrderAsync(customer, factory, "Card")).GetProperty("id").GetString();
        gateway.PaidInvoices.Add("invoice-1");

        var retried = await ReadAsync(await customer.PostAsync($"/orders/{orderId}/payment/retry", null));

        Assert.Equal("Confirmed", retried.GetProperty("status").GetString());
        Assert.Equal("Succeeded", retried.GetProperty("payment").GetProperty("status").GetString());
        Assert.Equal(1, gateway.InvoiceCount);
    }

    [Fact]
    public async Task Only_the_owner_of_an_unpaid_card_order_can_pay_again()
    {
        using var baseFactory = new ApiFactory();
        using var factory = WithGateway(baseFactory, new FakeGateway());
        var mona = await SignUpAsync(factory, "mona@example.com");
        var cardOrderId = (await PlaceOrderAsync(mona, factory, "Card")).GetProperty("id").GetString();
        var cashOrderId = (await PlaceOrderAsync(mona, factory, "CashOnDelivery")).GetProperty("id").GetString();
        var omar = await SignUpAsync(factory, "omar@example.com");

        Assert.Equal(HttpStatusCode.Conflict, (await mona.PostAsync($"/orders/{cashOrderId}/payment/retry", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await omar.PostAsync($"/orders/{cardOrderId}/payment/retry", null)).StatusCode);

        (await mona.PostAsync($"/orders/{cardOrderId}/cancel", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await mona.PostAsync($"/orders/{cardOrderId}/payment/retry", null)).StatusCode);
    }

    private static WebApplicationFactory<Program> WithGateway(ApiFactory factory, FakeGateway gateway) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IFawaterakGateway>(gateway)));

    // Each invoice gets the next number, and an invoice counts as paid once a test adds it to PaidInvoices
    private sealed class FakeGateway : IFawaterakGateway
    {
        private readonly ConcurrentDictionary<string, (Guid OrderId, decimal Total)> invoices = new();

        public ConcurrentBag<string> PaidInvoices { get; } = [];

        public int InvoiceCount => invoices.Count;

        public Task<FawaterakCheckout> CreateInvoiceAsync(Order order, string customerEmail, CancellationToken ct)
        {
            var invoiceId = $"invoice-{invoices.Count + 1}";
            invoices[invoiceId] = (order.Id, order.TotalAmount);
            return Task.FromResult(new FawaterakCheckout(invoiceId, $"key-{invoiceId}", $"https://pay.example.com/{invoiceId}"));
        }

        public Task<FawaterakInvoice> GetInvoiceAsync(string invoiceId, CancellationToken ct)
        {
            var (orderId, total) = invoices[invoiceId];
            return Task.FromResult(new FawaterakInvoice(invoiceId, orderId, total, PaidInvoices.Contains(invoiceId)));
        }

        public Task<FawaterakInvoice?> ReadPaidInvoiceAsync(string webhookBody, CancellationToken ct) => throw new NotSupportedException();
    }

    // Signs up a customer with a saved address
    private static async Task<HttpClient> SignUpAsync(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new { name = "Test Customer", email, password = Password });
        register.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", (await ReadAsync(register)).GetProperty("accessToken").GetString());

        (await client.PostAsJsonAsync("/addresses", new
        {
            fullAddressName = "Test Customer", street = "12 Tahrir Street", city = "Cairo",
            country = "Egypt", phone = "+201000000000", postalCode = (string?)null,
        })).EnsureSuccessStatusCode();
        return client;
    }

    // Puts a new product in the cart and places the order with the customer's first address
    private static async Task<JsonElement> PlaceOrderAsync(HttpClient customer, WebApplicationFactory<Program> factory, string paymentMethod)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", (await ReadAsync(login)).GetProperty("accessToken").GetString());
        var product = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = $"Test Speaker {Guid.NewGuid():N}", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock = 5, category = "Speakers", imageUrl = (string?)null, inTheBox = (object?)null,
        });
        product.EnsureSuccessStatusCode();
        var productId = (await ReadAsync(product)).GetProperty("id").GetString();

        (await customer.PostAsJsonAsync("/cart/items", new { productId, quantity = 1 })).EnsureSuccessStatusCode();
        var addressId = (await ReadAsync(await customer.GetAsync("/addresses")))[0].GetProperty("id").GetString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await customer.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync(response);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
