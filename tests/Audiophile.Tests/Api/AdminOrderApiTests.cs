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

// The admin order endpoints through real HTTP requests, cash on delivery orders start Confirmed
public sealed class AdminOrderApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task An_order_walks_from_confirmed_to_delivered()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);
        var orderId = await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 1);

        JsonElement order = default;
        foreach (var status in new[] { "Processing", "Shipped", "Delivered" })
        {
            var response = await ChangeStatusAsync(admin, orderId, status);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            order = await ReadAsync(response);
            Assert.Equal(status, order.GetProperty("status").GetString());
        }

        Assert.Equal(0, order.GetProperty("nextStatuses").GetArrayLength());
    }

    [Fact]
    public async Task A_status_that_is_not_next_is_refused()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);
        var orderId = await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 1);

        Assert.Equal(HttpStatusCode.Conflict, (await ChangeStatusAsync(admin, orderId, "Delivered")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ChangeStatusAsync(admin, orderId, "Pending")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ChangeStatusAsync(admin, orderId, "Lost")).StatusCode);
    }

    [Fact]
    public async Task An_admin_cancel_puts_the_stock_back()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);
        var orderId = await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 2);
        Assert.Equal(3, await StockAsync(admin, productId));

        var response = await ChangeStatusAsync(admin, orderId, "Cancelled");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cancelled", (await ReadAsync(response)).GetProperty("status").GetString());
        Assert.Equal(5, await StockAsync(admin, productId));
    }

    [Fact]
    public async Task An_unpaid_card_order_cannot_be_confirmed_by_hand_but_can_be_cancelled()
    {
        using var baseFactory = new ApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IFawaterakGateway, FakeGateway>()));
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);
        var orderId = await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 1, paymentMethod: "Card");

        var listed = (await ListAsync(admin, "?status=Pending")).GetProperty("items")[0];
        Assert.Equal("Cancelled", Assert.Single(listed.GetProperty("nextStatuses").EnumerateArray()).GetString());

        var confirm = await ChangeStatusAsync(admin, orderId, "Confirmed");
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);

        var cancel = await ChangeStatusAsync(admin, orderId, "Cancelled");
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Failed", (await ReadAsync(cancel)).GetProperty("paymentStatus").GetString());
    }

    [Fact]
    public async Task One_order_has_its_customer_address_and_items()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);
        var orderId = await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 2);

        var response = await admin.GetAsync($"/admin/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await ReadAsync(response);
        Assert.Equal("mona@example.com", order.GetProperty("customerEmail").GetString());
        Assert.Equal("Cairo", order.GetProperty("shipping").GetProperty("city").GetString());
        Assert.Equal(2, order.GetProperty("itemCount").GetInt32());
        Assert.Equal(2, order.GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Equal(["Processing", "Cancelled"],
            order.GetProperty("nextStatuses").EnumerateArray().Select(s => s.GetString()!).ToArray());

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/admin/orders/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task The_list_filters_by_status_search_and_dates_and_counts_every_status()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 10);
        await PlaceOrderAsync(factory, "mona@example.com", productId, quantity: 1);
        var omarOrderId = await PlaceOrderAsync(factory, "omar@example.com", productId, quantity: 3);
        await ChangeStatusAsync(admin, omarOrderId, "Cancelled");

        var everything = await ListAsync(admin, "");
        Assert.Equal(2, everything.GetProperty("totalCount").GetInt32());
        Assert.Equal(omarOrderId, everything.GetProperty("items")[0].GetProperty("id").GetString());

        var byEmail = await ListAsync(admin, "?q=OMAR@");
        Assert.Equal(omarOrderId, Assert.Single(Ids(byEmail)));
        Assert.Equal(3, byEmail.GetProperty("items")[0].GetProperty("itemCount").GetInt32());

        Assert.Equal(omarOrderId, Assert.Single(Ids(await ListAsync(admin, $"?q={omarOrderId[..8]}"))));

        var cancelled = await ListAsync(admin, "?status=Cancelled");
        Assert.Equal(omarOrderId, Assert.Single(Ids(cancelled)));
        var counts = cancelled.GetProperty("statusCounts");
        Assert.Equal(1, counts.GetProperty("Confirmed").GetInt32());
        Assert.Equal(1, counts.GetProperty("Cancelled").GetInt32());
        Assert.Equal(0, counts.GetProperty("Pending").GetInt32());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(2, (await ListAsync(admin, $"?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await ListAsync(admin, $"?to={today.AddDays(-1):yyyy-MM-dd}")).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task A_customer_gets_403_on_every_admin_order_endpoint()
    {
        using var factory = new ApiFactory();
        var customer = factory.CreateClient();
        Authorize(customer, await RegisterAsync(customer, "mona@example.com"));
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync($"/admin/orders/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ChangeStatusAsync(customer, id.ToString(), "Shipped")).StatusCode);
    }

    // Gives a card order a payment page without calling Fawaterak, nothing else is used by these tests
    private sealed class FakeGateway : IFawaterakGateway
    {
        public Task<FawaterakCheckout> CreateInvoiceAsync(Order order, string customerEmail, CancellationToken ct) =>
            Task.FromResult(new FawaterakCheckout("invoice-1", "key-1", "https://pay.example.com/invoice-1"));

        public Task<FawaterakInvoice> GetInvoiceAsync(string invoiceId, CancellationToken ct) => throw new NotSupportedException();

        public Task<FawaterakInvoice?> ReadPaidInvoiceAsync(string webhookBody, CancellationToken ct) => throw new NotSupportedException();
    }

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, string orderId, string status) =>
        client.PutAsJsonAsync($"/admin/orders/{orderId}/status", new { status });

    private static async Task<JsonElement> ListAsync(HttpClient admin, string query)
    {
        var response = await admin.GetAsync($"/admin/orders{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    private static IEnumerable<string> Ids(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(o => o.GetProperty("id").GetString()!).ToList();

    // Signs up a customer with an address, puts the product in the cart and places the order
    private static async Task<string> PlaceOrderAsync(
        WebApplicationFactory<Program> factory, string email, string productId, int quantity, string paymentMethod = "CashOnDelivery")
    {
        var client = factory.CreateClient();
        Authorize(client, await RegisterAsync(client, email));

        var address = await client.PostAsJsonAsync("/addresses", new
        {
            fullAddressName = "Test Customer", street = "12 Tahrir Street", city = "Cairo",
            country = "Egypt", phone = "+201000000000", postalCode = (string?)null,
        });
        address.EnsureSuccessStatusCode();
        var addressId = (await ReadAsync(address)).GetProperty("id").GetString()!;

        (await client.PostAsJsonAsync("/cart/items", new { productId, quantity })).EnsureSuccessStatusCode();

        var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task<int> StockAsync(HttpClient admin, string productId) =>
        (await ReadAsync(await admin.GetAsync($"/products/{productId}"))).GetProperty("stock").GetInt32();

    private static async Task<HttpClient> AdminClientAsync(WebApplicationFactory<Program> factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    private static async Task<string> CreateProductAsync(HttpClient admin, int stock)
    {
        var response = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = "Test Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock, category = "Speakers", imageUrl = (string?)null, inTheBox = (object?)null,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task<string> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { name = "Test Customer", email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).GetProperty("accessToken").GetString()!;
    }

    private static void Authorize(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
