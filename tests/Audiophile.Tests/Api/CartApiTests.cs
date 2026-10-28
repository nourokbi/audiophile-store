using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Audiophile.Tests.Api;

// The cart a visitor keeps in the browser: priced without an account, then moved into the account after login
public sealed class CartApiTests
{
    [Fact]
    public async Task A_visitor_gets_the_cart_priced_without_logging_in()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 5);

        var cart = await QuoteAsync(factory.CreateClient(), new { productId, quantity = 2 });

        Assert.Equal(2, cart.GetProperty("totalQuantity").GetInt32());
        Assert.Equal(2000m, cart.GetProperty("subTotal").GetDecimal());
    }

    [Fact]
    public async Task The_quote_leaves_out_a_hidden_product_and_caps_a_line_at_the_stock()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var shownId = await CreateProductAsync(admin, stock: 3);
        var hiddenId = await CreateProductAsync(admin, stock: 5);
        (await admin.PutAsJsonAsync($"/admin/products/{hiddenId}/availability", new { isActive = false })).EnsureSuccessStatusCode();

        var cart = await QuoteAsync(factory.CreateClient(),
            new { productId = shownId, quantity = 10 }, new { productId = hiddenId, quantity = 1 });

        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(shownId, item.GetProperty("productId").GetString());
        Assert.Equal(3, item.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Merging_tops_up_a_line_already_in_the_account_cart()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(await AdminClientAsync(factory), stock: 10);
        var customer = await CustomerClientAsync(factory);
        (await customer.PostAsJsonAsync("/cart/items", new { productId, quantity = 1 })).EnsureSuccessStatusCode();

        var cart = await MergeAsync(customer, new { productId, quantity = 2 });

        Assert.Equal(3, Assert.Single(cart.GetProperty("items").EnumerateArray()).GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Merging_more_than_the_stock_caps_the_line_instead_of_failing()
    {
        using var factory = new ApiFactory();
        var productId = await CreateProductAsync(await AdminClientAsync(factory), stock: 4);
        var customer = await CustomerClientAsync(factory);
        (await customer.PostAsJsonAsync("/cart/items", new { productId, quantity = 3 })).EnsureSuccessStatusCode();

        var cart = await MergeAsync(customer, new { productId, quantity = 5 });

        Assert.Equal(4, Assert.Single(cart.GetProperty("items").EnumerateArray()).GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Merging_needs_a_signed_in_customer()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/cart/merge", Array.Empty<object>());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<JsonElement> QuoteAsync(HttpClient client, params object[] items)
    {
        var response = await client.PostAsJsonAsync("/cart/quote", items);
        response.EnsureSuccessStatusCode();
        return await ReadAsync(response);
    }

    private static async Task<JsonElement> MergeAsync(HttpClient client, params object[] items)
    {
        var response = await client.PostAsJsonAsync("/cart/merge", items);
        response.EnsureSuccessStatusCode();
        return await ReadAsync(response);
    }

    private static async Task<string> CreateProductAsync(HttpClient admin, int stock)
    {
        var response = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = $"Test Speaker {Guid.NewGuid():N}", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock, category = "Speakers", images = (string[]?)null, inTheBox = (object?)null,
        });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    private static async Task<HttpClient> CustomerClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new { name = "Test Customer", email = "mona@example.com", password = "password-123" });
        response.EnsureSuccessStatusCode();
        Authorize(client, (await ReadAsync(response)).GetProperty("accessToken").GetString()!);
        return client;
    }

    private static void Authorize(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
