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

        var all = await ReadAsync(await admin.GetAsync("/admin/products?availability=Hidden"));
        var hidden = all.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetString() == id);
        Assert.False(hidden.GetProperty("isActive").GetBoolean());
        Assert.Contains(id, Ids(await ReadAsync(await admin.GetAsync("/admin/products/search?q=hidden"))));
    }

    [Fact]
    public async Task The_shop_list_combines_search_category_price_and_stock()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var wanted = await CreateProductAsync(admin, "Zephyr Wanted Speaker", price: 2000m, stock: 3);
        await CreateProductAsync(admin, "Zephyr Sold Out Speaker", price: 2000m, stock: 0);
        await CreateProductAsync(admin, "Zephyr Costly Speaker", price: 9000m, stock: 3);
        await CreateProductAsync(admin, "Zephyr Wrong Category", price: 2000m, stock: 3, category: "Amplifiers");
        await CreateProductAsync(admin, "Plain Speaker", price: 2000m, stock: 3);

        var page = await ReadAsync(await factory.CreateClient().GetAsync(
            "/products?q=zephyr&category=Speakers&minPrice=1000&maxPrice=3000&inStockOnly=true"));

        Assert.Equal(wanted, Assert.Single(Ids(page)));
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("PriceLowToHigh", "Zephyr Cheap", "Zephyr Middle", "Zephyr Costly")]
    [InlineData("PriceHighToLow", "Zephyr Costly", "Zephyr Middle", "Zephyr Cheap")]
    [InlineData("Name", "Zephyr Cheap", "Zephyr Costly", "Zephyr Middle")]
    public async Task The_shop_list_sorts(string sort, string first, string second, string third)
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        await CreateProductAsync(admin, "Zephyr Middle", price: 2000m);
        await CreateProductAsync(admin, "Zephyr Cheap", price: 1000m);
        await CreateProductAsync(admin, "Zephyr Costly", price: 3000m);

        var page = await ReadAsync(await factory.CreateClient().GetAsync($"/products?q=zephyr&sort={sort}"));

        Assert.Equal([first, second, third], Names(page));
    }

    [Fact]
    public async Task Featured_shows_the_new_collection_first_and_new_only_keeps_just_those()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var featured = await ReadAsync(await client.GetAsync("/products?sort=Featured&pageSize=50"));
        var isNew = featured.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("isNew").GetBoolean()).ToList();
        var newOnly = await ReadAsync(await client.GetAsync("/products?newOnly=true&pageSize=50"));

        Assert.Equal(16, newOnly.GetProperty("totalCount").GetInt32());
        Assert.All(isNew.Take(16), Assert.True);
        Assert.All(isNew.Skip(16), Assert.False);
    }

    [Theory]
    [InlineData(72, HttpStatusCode.OK)]
    [InlineData(73, HttpStatusCode.BadRequest)]
    public async Task The_largest_page_is_the_largest_size_the_shop_offers(int pageSize, HttpStatusCode expected)
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync($"/products?pageSize={pageSize}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("ZEPHYR")]
    [InlineData("phyr spe")]
    [InlineData("tested for")]
    public async Task Search_finds_any_part_of_the_name_or_description_in_any_case(string term)
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, "Zephyr Speaker", description: "A speaker tested for long sessions.");

        var page = await ReadAsync(await factory.CreateClient().GetAsync($"/products?q={Uri.EscapeDataString(term)}"));

        Assert.Equal(id, Assert.Single(Ids(page)));
    }

    [Fact]
    public async Task An_unknown_sort_is_400()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync("/products?sort=Nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_product_is_found_by_its_slug_unless_it_is_hidden()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, "Zephyr Slug Speaker");
        var client = factory.CreateClient();

        var found = await client.GetAsync("/products/by-slug/zephyr-slug-speaker");
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(id, (await ReadAsync(found)).GetProperty("id").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/products/by-slug/no-such-product")).StatusCode);

        (await admin.DeleteAsync($"/admin/products/{id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/products/by-slug/zephyr-slug-speaker")).StatusCode);
    }

    [Fact]
    public async Task Related_products_leave_out_the_product_and_put_its_category_first()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, "Zephyr Amplifier One", category: "Amplifiers");
        var client = factory.CreateClient();

        var related = await ReadAsync(await client.GetAsync($"/products/{id}/related?count=3"));

        var products = related.EnumerateArray().ToList();
        Assert.Equal(3, products.Count);
        Assert.DoesNotContain(products, p => p.GetProperty("id").GetString() == id);
        Assert.All(products, p => Assert.Equal("Amplifiers", p.GetProperty("category").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/products/{id}/related?count=0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/products/{Guid.NewGuid()}/related")).StatusCode);
    }

    private static IEnumerable<string> Names(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToList();

    private static IEnumerable<string> Ids(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetString()!).ToList();

    private static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    private static async Task<string> CreateProductAsync(
        HttpClient admin, string name, decimal price = 1000m, int stock = 5, string category = "Speakers",
        string description = "A speaker made for the tests.")
    {
        var response = await admin.PostAsJsonAsync("/admin/products", new
        {
            name, description, features = (string?)null,
            price, stock, category, images = (string[]?)null, inTheBox = (object?)null,
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
