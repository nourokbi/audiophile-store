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

// A paid card order cancelled before it is prepared, then refunded by the admin, with a fake Fawaterak
public sealed class RefundApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task A_paid_order_cancelled_by_the_customer_is_refunded_once_and_both_sides_are_told()
    {
        using var baseFactory = new ApiFactory();
        var gateway = new FakeGateway();
        using var factory = WithGateway(baseFactory, gateway);
        var admin = await AdminClientAsync(factory);
        var customer = await SignUpAsync(factory, "mona@example.com");
        var orderId = await PlacePaidCardOrderAsync(factory, gateway, customer, admin);
        var number = "#" + orderId.Replace("-", "")[^8..].ToUpperInvariant();

        var cancelled = await customer.PostAsync($"/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Succeeded", (await ReadAsync(cancelled)).GetProperty("payment").GetProperty("status").GetString());
        var cancelEmail = await baseFactory.Emails.WaitForAsync(e => e.Subject == $"Order {number} was cancelled");
        Assert.Contains("will go back to your card", cancelEmail.Text);
        await WaitForNotificationAsync(admin, $"Order {number} cancelled, refund needed");

        var refund = await admin.PostAsJsonAsync($"/admin/orders/{orderId}/refund", new { reference = "RF-123" });

        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);
        var refunded = await ReadAsync(refund);
        Assert.Equal("Refunded", refunded.GetProperty("paymentStatus").GetString());
        Assert.Equal("RF-123", refunded.GetProperty("refundReference").GetString());
        Assert.NotEqual(JsonValueKind.Null, refunded.GetProperty("refundedAt").ValueKind);
        var refundEmail = await baseFactory.Emails.WaitForAsync(e => e.Subject == $"Refund for order {number} sent");
        Assert.Contains("EGP 1,050.00", refundEmail.Text);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/admin/orders/{orderId}/refund", new { reference = (string?)null })).StatusCode);
        var events = await ReadAsync(await admin.GetAsync($"/orders/{orderId}/payment/events"));
        Assert.Contains(events.EnumerateArray(), e => e.GetProperty("eventType").GetString() == "admin:refunded");
        Assert.Equal(0m, (await ReadAsync(await admin.GetAsync("/admin/orders/stats"))).GetProperty("revenue").GetDecimal());
    }

    [Fact]
    public async Task Only_a_cancelled_paid_card_order_can_be_refunded()
    {
        using var baseFactory = new ApiFactory();
        var gateway = new FakeGateway();
        using var factory = WithGateway(baseFactory, gateway);
        var admin = await AdminClientAsync(factory);
        var customer = await SignUpAsync(factory, "mona@example.com");

        var paidId = await PlacePaidCardOrderAsync(factory, gateway, customer, admin);
        Assert.Equal(HttpStatusCode.Conflict, (await RefundAsync(admin, paidId)).StatusCode);

        var cashId = (await PlaceOrderAsync(customer, admin, factory, "CashOnDelivery")).GetProperty("id").GetString();
        (await customer.PostAsync($"/orders/{cashId}/cancel", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await RefundAsync(admin, cashId!)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await RefundAsync(admin, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RefundAsync(customer, paidId)).StatusCode);
    }

    [Fact]
    public async Task A_paid_order_cannot_be_cancelled_by_the_customer_once_it_is_being_prepared()
    {
        using var baseFactory = new ApiFactory();
        var gateway = new FakeGateway();
        using var factory = WithGateway(baseFactory, gateway);
        var admin = await AdminClientAsync(factory);
        var customer = await SignUpAsync(factory, "mona@example.com");
        var orderId = await PlacePaidCardOrderAsync(factory, gateway, customer, admin);

        (await admin.PutAsJsonAsync($"/admin/orders/{orderId}/status", new { status = "Processing" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsync($"/orders/{orderId}/cancel", null)).StatusCode);
    }

    private static Task<HttpResponseMessage> RefundAsync(HttpClient client, string orderId) =>
        client.PostAsJsonAsync($"/admin/orders/{orderId}/refund", new { reference = (string?)null });

    // Places a card order, marks its invoice paid at the fake Fawaterak, and lets the customer's retry confirm it
    private static async Task<string> PlacePaidCardOrderAsync(
        WebApplicationFactory<Program> factory, FakeGateway gateway, HttpClient customer, HttpClient admin)
    {
        var order = await PlaceOrderAsync(customer, admin, factory, "Card");
        var orderId = order.GetProperty("id").GetString()!;
        gateway.PaidInvoices.Add(gateway.LastInvoiceId);

        var paid = await ReadAsync(await customer.PostAsync($"/orders/{orderId}/payment/retry", null));
        Assert.Equal("Confirmed", paid.GetProperty("status").GetString());
        return orderId;
    }

    private static WebApplicationFactory<Program> WithGateway(ApiFactory factory, FakeGateway gateway) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IFawaterakGateway>(gateway)));

    // Each invoice gets the next number, and an invoice counts as paid once a test adds it to PaidInvoices
    private sealed class FakeGateway : IFawaterakGateway
    {
        private readonly ConcurrentDictionary<string, (Guid OrderId, decimal Total)> invoices = new();

        public ConcurrentBag<string> PaidInvoices { get; } = [];

        public string LastInvoiceId => $"invoice-{invoices.Count}";

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

    private static async Task<JsonElement> WaitForNotificationAsync(HttpClient admin, string title)
    {
        for (var waited = 0; waited < 20_000; waited += 200)
        {
            var list = await ReadAsync(await admin.GetAsync("/admin/notifications"));
            foreach (var notification in list.GetProperty("items").EnumerateArray())
                if (notification.GetProperty("title").GetString() == title)
                    return notification;
            await Task.Delay(200);
        }
        throw new TimeoutException($"The admin never got \"{title}\"");
    }

    private static async Task<HttpClient> AdminClientAsync(WebApplicationFactory<Program> factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    // Signs up a customer with a saved address
    private static async Task<HttpClient> SignUpAsync(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new { name = "Test Customer", email, password = Password });
        register.EnsureSuccessStatusCode();
        Authorize(client, (await ReadAsync(register)).GetProperty("accessToken").GetString()!);

        (await client.PostAsJsonAsync("/addresses", new
        {
            fullAddressName = "Test Customer", street = "12 Tahrir Street", city = "Cairo",
            country = "Egypt", phone = "+201000000000", postalCode = (string?)null,
        })).EnsureSuccessStatusCode();
        return client;
    }

    // Creates a product at 1000 with 50 shipping, puts one in the cart and places the order with the saved address
    private static async Task<JsonElement> PlaceOrderAsync(
        HttpClient customer, HttpClient admin, WebApplicationFactory<Program> factory, string paymentMethod)
    {
        var product = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = $"Test Speaker {Guid.NewGuid():N}", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock = 20, category = "Speakers", images = (string[]?)null, inTheBox = (object?)null,
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
        response.EnsureSuccessStatusCode();
        return await ReadAsync(response);
    }

    private static void Authorize(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
