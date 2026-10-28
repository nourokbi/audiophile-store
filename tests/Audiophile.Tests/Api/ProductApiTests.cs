using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Audiophile.Tests.Api;

// Listing and searching the catalogue, and the hidden products only an admin may list
public sealed class ProductApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task Search_finds_products_by_name_and_pages_the_result()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        await CreateProductAsync(admin, "Zephyr Speaker One");
        await CreateProductAsync(admin, "Zephyr Speaker Two");
        await CreateProductAsync(admin, "Plain Speaker");

        var page = await ReadAsync(await factory.CreateClient().GetAsync("/products/search?q=zephyr&pageSize=1"));

        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, page.GetProperty("items").GetArrayLength());
        Assert.Equal("Zephyr Speaker One", page.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Search_without_a_term_is_400()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync("/products/search?q=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_an_admin_can_list_hidden_products()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, "Hidden Speaker");
        (await admin.DeleteAsync($"/admin/products/{id}")).EnsureSuccessStatusCode();

        var customer = factory.CreateClient();
        Authorize(customer, await RegisterAsync(customer, "mona@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/admin/products/search?q=hidden")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/admin/products")).StatusCode);
        Assert.DoesNotContain(id, Ids(await ReadAsync(await customer.GetAsync("/products?pageSize=50"))));
        Assert.DoesNotContain(id, Ids(await ReadAsync(await customer.GetAsync("/products/search?q=hidden"))));

        var all = await ReadAsync(await admin.GetAsync("/admin/products?pageSize=50"));
        var hidden = all.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetString() == id);
        Assert.False(hidden.GetProperty("isActive").GetBoolean());
        Assert.Contains(id, Ids(await ReadAsync(await admin.GetAsync("/admin/products/search?q=hidden"))));
    }

    private static IEnumerable<string> Ids(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetString()!).ToList();

    private static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    private static async Task<string> CreateProductAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync("/admin/products", new
        {
            name, description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock = 5, category = "Speakers", images = (string[]?)null, inTheBox = (object?)null,
        });
        response.EnsureSuccessStatusCode();
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
