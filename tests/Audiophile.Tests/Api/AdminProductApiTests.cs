using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Audiophile.Tests.Api;

// The admin product endpoints through real HTTP requests
public sealed class AdminProductApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task Restock_adds_the_quantity_to_the_stock()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);

        var response = await admin.PostAsJsonAsync($"/admin/products/{id}/restock", new { quantity = 5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, (await ReadAsync(response)).GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task A_product_keeps_its_images_in_order_and_the_first_is_the_cover()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        string[] images = ["/assets/products/front.jpg", "https://images.example.com/side.jpg", " ", "/assets/products/front.jpg"];

        var created = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = "Test Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock = 3, category = "Speakers", images, inTheBox = (object?)null,
        });
        var product = await ReadAsync(created);
        var id = product.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(["/assets/products/front.jpg", "https://images.example.com/side.jpg"], ImagesOf(product));
        Assert.Equal("/assets/products/front.jpg", product.GetProperty("imageUrl").GetString());

        var reordered = await admin.PutAsJsonAsync($"/admin/products/{id}", new
        {
            name = "Test Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, category = "Speakers", images = new[] { "https://images.example.com/side.jpg", "/assets/products/front.jpg" },
            inTheBox = (object?)null,
        });
        var edited = await ReadAsync(reordered);
        Assert.Equal("https://images.example.com/side.jpg", edited.GetProperty("imageUrl").GetString());
        Assert.Equal(["https://images.example.com/side.jpg", "/assets/products/front.jpg"], ImagesOf(edited));

        var tooMany = await admin.PostAsJsonAsync("/admin/products", new
        {
            name = "Another Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1000m, stock = 3, category = "Speakers", images = Enumerable.Range(1, 9).Select(n => $"/assets/products/{n}.jpg"),
            inTheBox = (object?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
    }

    [Fact]
    public async Task Change_price_sets_the_new_price()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);

        var response = await admin.PutAsJsonAsync($"/admin/products/{id}/price", new { price = 1499.5m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1499.5m, (await ReadAsync(response)).GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task A_hidden_product_leaves_the_shop_and_comes_back_when_made_available()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);
        var shopper = factory.CreateClient();

        var hidden = await admin.PutAsJsonAsync($"/admin/products/{id}/availability", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        Assert.False((await ReadAsync(hidden)).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await shopper.GetAsync($"/products/{id}")).StatusCode);
        Assert.Contains(id, Ids(await ReadAsync(await admin.GetAsync("/admin/products?availability=Hidden"))));

        // A hidden product can still be restocked, so the admin can get it ready before showing it again
        (await admin.PostAsJsonAsync($"/admin/products/{id}/restock", new { quantity = 1 })).EnsureSuccessStatusCode();

        (await admin.PutAsJsonAsync($"/admin/products/{id}/availability", new { isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await shopper.GetAsync($"/products/{id}")).StatusCode);
    }

    [Fact]
    public async Task An_admin_can_edit_a_hidden_product()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);
        (await admin.DeleteAsync($"/admin/products/{id}")).EnsureSuccessStatusCode();

        var response = await admin.PutAsJsonAsync($"/admin/products/{id}", EditedProduct("Renamed Speaker"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await ReadAsync(response);
        Assert.Equal("Renamed Speaker", product.GetProperty("name").GetString());
        Assert.False(product.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task An_edit_never_changes_the_stock_and_keeps_the_box_when_it_is_left_out()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);
        (await admin.PutAsJsonAsync($"/admin/products/{id}", EditedProduct("Boxed Speaker", new Dictionary<string, int> { ["Speaker"] = 2, ["Cable"] = 1 })))
            .EnsureSuccessStatusCode();

        // An edit that still sends the old stock number, as a form opened before a checkout would
        var response = await admin.PutAsJsonAsync($"/admin/products/{id}", new
        {
            name = "Boxed Speaker", description = "A speaker made for the tests.", features = (string?)null,
            price = 1200m, stock = 99, category = "Speakers", images = (string[]?)null,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await ReadAsync(response);
        Assert.Equal(3, product.GetProperty("stock").GetInt32());
        Assert.Equal(1200m, product.GetProperty("price").GetDecimal());
        Assert.Equal(2, product.GetProperty("inTheBox").GetProperty("Speaker").GetInt32());
    }

    [Fact]
    public async Task Bad_input_is_400_and_an_unknown_product_is_404()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/admin/products/{id}/restock", new { quantity = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/admin/products/{id}/price", new { price = 0m })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PostAsJsonAsync($"/admin/products/{Guid.NewGuid()}/restock", new { quantity = 5 })).StatusCode);
    }

    [Fact]
    public async Task A_customer_gets_403_and_a_guest_gets_401_on_every_admin_product_endpoint()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var id = await CreateProductAsync(admin, stock: 3);
        var customer = factory.CreateClient();
        Authorize(customer, await RegisterAsync(customer, "mona@example.com"));
        var guest = factory.CreateClient();

        foreach (var (client, expected) in new[] { (customer, HttpStatusCode.Forbidden), (guest, HttpStatusCode.Unauthorized) })
        {
            Assert.Equal(expected, (await client.PostAsJsonAsync("/admin/products", NewProduct(3))).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync($"/admin/products/{id}", NewProduct(3))).StatusCode);
            Assert.Equal(expected, (await client.DeleteAsync($"/admin/products/{id}")).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync($"/admin/products/{id}/restock", new { quantity = 5 })).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync($"/admin/products/{id}/price", new { price = 10m })).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync($"/admin/products/{id}/availability", new { isActive = false })).StatusCode);
        }
    }

    [Fact]
    public async Task The_admin_list_filters_by_availability_and_low_stock()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var hiddenLowStock = await CreateProductAsync(admin, stock: 3, name: "Filter Hidden Speaker");
        var onSaleStocked = await CreateProductAsync(admin, stock: 20, name: "Filter Stocked Speaker");
        (await admin.DeleteAsync($"/admin/products/{hiddenLowStock}")).EnsureSuccessStatusCode();

        var hidden = Ids(await ReadAsync(await admin.GetAsync("/admin/products?availability=Hidden&q=Filter")));
        var onSale = Ids(await ReadAsync(await admin.GetAsync("/admin/products?availability=OnSale&q=Filter")));
        var lowStock = Ids(await ReadAsync(await admin.GetAsync("/admin/products?lowStockOnly=true&q=Filter")));

        Assert.Contains(hiddenLowStock, hidden);
        Assert.DoesNotContain(onSaleStocked, hidden);
        Assert.Contains(onSaleStocked, onSale);
        Assert.DoesNotContain(hiddenLowStock, onSale);
        Assert.Contains(hiddenLowStock, lowStock);
        Assert.DoesNotContain(onSaleStocked, lowStock);
    }

    [Fact]
    public async Task The_admin_list_searches_and_sorts_and_refuses_an_unknown_sort()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var expensive = await CreateProductAsync(admin, stock: 5, name: "Sorter Alpha Speaker", price: 3000m);
        var cheap = await CreateProductAsync(admin, stock: 5, name: "Sorter Beta Speaker", price: 1000m);

        var lowToHigh = Ids(await ReadAsync(await admin.GetAsync("/admin/products?q=sorter&sort=PriceLowToHigh")));
        var highToLow = Ids(await ReadAsync(await admin.GetAsync("/admin/products?q=sorter&sort=PriceHighToLow")));

        Assert.Equal([cheap, expensive], lowToHigh);
        Assert.Equal([expensive, cheap], highToLow);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/admin/products?sort=Banana")).StatusCode);
    }

    private static List<string?> ImagesOf(JsonElement product) =>
        product.GetProperty("images").EnumerateArray().Select(i => i.GetString()).ToList();

    private static IEnumerable<string> Ids(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetString()!).ToList();

    private static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Authorize(admin, (await ReadAsync(login)).GetProperty("accessToken").GetString()!);
        return admin;
    }

    private static object NewProduct(int stock, string name = "Test Speaker", decimal price = 1000m) => new
    {
        name, description = "A speaker made for the tests.", features = (string?)null,
        price, stock, category = "Speakers", images = (string[]?)null, inTheBox = (object?)null,
    };

    private static object EditedProduct(string name, object? inTheBox = null) => new
    {
        name, description = "A speaker made for the tests.", features = (string?)null,
        price = 1000m, category = "Speakers", images = (string[]?)null, inTheBox,
    };

    private static async Task<string> CreateProductAsync(HttpClient admin, int stock, string name = "Test Speaker", decimal price = 1000m)
    {
        var response = await admin.PostAsJsonAsync("/admin/products", NewProduct(stock, name, price));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("/products/", response.Headers.Location!.ToString());
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
