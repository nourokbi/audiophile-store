using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Audiophile.Tests.Api;

// The customer's emails and the admin's bell, both sent through the message bus after the order is saved
public sealed class NotificationApiTests
{
    private const string Password = "password-123";

    [Fact]
    public async Task A_cash_order_emails_the_customer_and_tells_the_admin()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 20);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 2);

        var orderId = await PlaceOrderAsync(customer.Client, customer.AddressId);
        var number = "#" + orderId.Replace("-", "")[^8..].ToUpperInvariant();

        var email = await factory.Emails.WaitForAsync(e => e.Subject == $"Order {number} is confirmed");
        Assert.Equal("mona@example.com", email.ToAddress);
        Assert.Contains("EGP 2,000.00", email.Text);
        Assert.Contains($"/orders/{orderId}", email.Html);

        var bell = await WaitForNotificationAsync(admin, $"New order {number}");
        Assert.Equal("NewOrder", bell.GetProperty("kind").GetString());
        Assert.Equal(orderId, bell.GetProperty("orderId").GetString());
        Assert.False(bell.GetProperty("isRead").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync("/admin/notifications/read-all", null)).StatusCode);
        Assert.Equal(0, (await ReadAsync(await admin.GetAsync("/admin/notifications"))).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Every_step_the_admin_takes_emails_the_customer_once()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 20);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 1);
        var orderId = await PlaceOrderAsync(customer.Client, customer.AddressId);
        var number = "#" + orderId.Replace("-", "")[^8..].ToUpperInvariant();

        foreach (var status in new[] { "Processing", "Shipped", "Delivered" })
            (await admin.PutAsJsonAsync($"/admin/orders/{orderId}/status", new { status })).EnsureSuccessStatusCode();

        await factory.Emails.WaitForAsync(e => e.Subject == $"We're preparing order {number}");
        await factory.Emails.WaitForAsync(e => e.Subject == $"Order {number} is on its way");
        await factory.Emails.WaitForAsync(e => e.Subject == $"Order {number} was delivered");
        Assert.Equal(4, factory.Emails.All.Count);
    }

    [Fact]
    public async Task A_customer_cancelling_emails_them_and_tells_the_admin()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 20);
        var customer = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 1);
        var orderId = await PlaceOrderAsync(customer.Client, customer.AddressId);
        var number = "#" + orderId.Replace("-", "")[^8..].ToUpperInvariant();

        (await customer.Client.PostAsync($"/orders/{orderId}/cancel", null)).EnsureSuccessStatusCode();

        await factory.Emails.WaitForAsync(e => e.Subject == $"Order {number} was cancelled");
        var bell = await WaitForNotificationAsync(admin, $"Order {number} cancelled");
        Assert.Equal("OrderCancelled", bell.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Selling_down_to_the_low_stock_limit_tells_the_admin_once()
    {
        using var factory = new ApiFactory();
        var admin = await AdminClientAsync(factory);
        var productId = await CreateProductAsync(admin, stock: 10);
        var mona = await SignUpWithCartAsync(factory, "mona@example.com", productId, quantity: 2);
        await PlaceOrderAsync(mona.Client, mona.AddressId);
        var omar = await SignUpWithCartAsync(factory, "omar@example.com", productId, quantity: 1);
        await PlaceOrderAsync(omar.Client, omar.AddressId);

        var bell = await WaitForNotificationAsync(admin, "Low stock: Test Speaker");
        Assert.Equal("Only 8 left, restock it soon.", bell.GetProperty("message").GetString());
        Assert.Equal(productId, bell.GetProperty("productId").GetString());

        await WaitForNotificationAsync(admin, "New order", count: 2);
        var all = (await ReadAsync(await admin.GetAsync("/admin/notifications"))).GetProperty("items").EnumerateArray();
        Assert.Single(all, n => n.GetProperty("kind").GetString() == "LowStock");
    }

    [Fact]
    public async Task Only_an_admin_can_read_the_notifications()
    {
        using var factory = new ApiFactory();
        var customer = factory.CreateClient();
        Authorize(customer, await RegisterAsync(customer, "mona@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/admin/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/admin/notifications")).StatusCode);
    }

    // Waits until the bell has count notifications whose title starts with the text, and returns the newest
    private static async Task<JsonElement> WaitForNotificationAsync(HttpClient admin, string titleStart, int count = 1)
    {
        for (var waited = 0; waited < 20_000; waited += 200)
        {
            var list = await ReadAsync(await admin.GetAsync("/admin/notifications"));
            var matches = list.GetProperty("items").EnumerateArray()
                .Where(n => n.GetProperty("title").GetString()!.StartsWith(titleStart, StringComparison.Ordinal))
                .ToList();
            if (matches.Count >= count)
                return matches[0];
            await Task.Delay(200);
        }
        throw new TimeoutException($"The admin never got \"{titleStart}\"");
    }

    private static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
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
            price = 1000m, stock, category = "Speakers", images = (string[]?)null, inTheBox = (object?)null,
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

    private static async Task<string> PlaceOrderAsync(HttpClient client, string addressId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "CashOnDelivery" }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
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
