using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Audiophile.Tests.Api;

// Checkout, reading and cancelling orders through real HTTP requests, paid with cash on delivery so no gateway is needed
public sealed class OrderApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task Checkout_turns_the_cart_into_a_confirmed_order_and_empties_the_cart()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(factory, stock: 5);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 2);

        var response = await PlaceOrderAsync(customer.Client, customer.AddressId, idempotencyKey: "order-1");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await ReadAsync(response);
        Assert.Equal("Confirmed", order.GetProperty("status").GetString());
        Assert.Equal(2, order.GetProperty("items")[0].GetProperty("quantity").GetInt32());

        var cart = await ReadAsync(await customer.Client.GetAsync("/cart"));
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
        Assert.Equal(3, await StockAsync(factory, productId));
    }

    [Fact]
    public async Task The_same_idempotency_key_returns_the_same_order()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(factory, stock: 5);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 1);

        var first = await PlaceOrderAsync(customer.Client, customer.AddressId, idempotencyKey: "order-1");
        var again = await PlaceOrderAsync(customer.Client, customer.AddressId, idempotencyKey: "order-1");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal((await ReadAsync(first)).GetProperty("id").GetString(), (await ReadAsync(again)).GetProperty("id").GetString());
        Assert.Equal(4, await StockAsync(factory, productId));
    }

    [Fact]
    public async Task A_customer_cannot_read_or_cancel_another_customers_order()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(factory, stock: 5);
        var mona = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 1);
        var orderId = (await ReadAsync(await PlaceOrderAsync(mona.Client, mona.AddressId, "order-1"))).GetProperty("id").GetString();

        var omar = factory.CreateClient();
        Authorize(omar, await RegisterAsync(omar, "omar@example.com"));

        Assert.Equal(HttpStatusCode.NotFound, (await omar.GetAsync($"/orders/{orderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await omar.PostAsync($"/orders/{orderId}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Cancelling_returns_the_stock_and_a_second_cancel_is_refused()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(factory, stock: 5);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 2);
        var orderId = (await ReadAsync(await PlaceOrderAsync(customer.Client, customer.AddressId, "order-1"))).GetProperty("id").GetString();

        var cancelled = await customer.Client.PostAsync($"/orders/{orderId}/cancel", null);
        var again = await customer.Client.PostAsync($"/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Cancelled", (await ReadAsync(cancelled)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(5, await StockAsync(factory, productId));
    }

    [Fact]
    public async Task Two_customers_buying_the_last_unit_at_once_get_exactly_one_order()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(factory, stock: 1);
        var mona = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 1);
        var omar = await SignUpWithCartAsync(factory, "omar@example.com", productId, quantity: 1);

        var responses = await Task.WhenAll(
            PlaceOrderAsync(mona.Client, mona.AddressId, "order-mona"),
            PlaceOrderAsync(omar.Client, omar.AddressId, "order-omar"));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(0, await StockAsync(factory, productId));
    }

    private static async Task<string> CreateProductAsync(ApiFactory factory, int stock)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);

        var response = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = "Test Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock, category = "Speakers", imageUrl = (string?)null, inTheBox = (object?)null,
        });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).GetProperty("id").GetString()!;
    }

    // Signs up a customer, saves an address and puts the product in the cart
    private static async Task<(HttpClient Client, string AddressId)> SignUpWithCartAsync(
        ApiFactory factory, string email, string productId, int quantity)
    {
        var client = factory.CreateClient();
        Authorize(client, await RegisterAsync(client, email));

        var address = await client.PostAsJsonAsync("/addresses", new
        {
            fullAddressName = "Test Customer", street = "12 Tahrir Street", city = "Cairo",
            country = "Egypt", phone = "+201000000000", postalCode = (string?)null,
        });
        address.EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync("/cart/items", new { productId, quantity })).EnsureSuccessStatusCode();
        return (client, (await ReadAsync(address)).GetProperty("id").GetString()!);
    }

    private static Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client, string addressId, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "CashOnDelivery" }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private static async Task<int> StockAsync(ApiFactory factory, string productId) =>
        (await ReadAsync(await factory.CreateClient().GetAsync($"/products/{productId}"))).GetProperty("stock").GetInt32();

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
