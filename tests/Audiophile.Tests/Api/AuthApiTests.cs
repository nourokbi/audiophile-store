using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Audiophile.Tests.Api;

// Real HTTP requests through the whole API down to the database, each test gets a fresh app and database
public sealed class AuthApiTests
{
    private const string Password = "password-123";
    private const string RefreshCookie = "audiophile_refresh";

    [Fact]
    public async Task Register_returns_201_with_a_session_that_works()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register",
            new { name = "Mona Hassan", email = "mona@example.com", password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await ReadAsync(response);
        Authorize(client, session.GetProperty("accessToken").GetString()!);

        var me = await ReadAsync(await client.GetAsync("/auth/me"));
        Assert.Equal("mona@example.com", me.GetProperty("email").GetString());
        Assert.Equal("Customer", me.GetProperty("roles")[0].GetString());
    }

    [Fact]
    public async Task Registering_the_same_email_twice_is_a_conflict()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await RegisterAsync(client, "mona@example.com");

        var again = await client.PostAsJsonAsync("/auth/register",
            new { name = "Mona Again", email = "mona@example.com", password = Password });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Login_does_not_reveal_whether_the_email_exists()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await RegisterAsync(client, "mona@example.com");

        var wrongPassword = await client.PostAsJsonAsync("/auth/login", new { email = "mona@example.com", password = "wrong-pass-1" });
        var unknownEmail = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@example.com", password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(
            (await ReadAsync(wrongPassword)).GetProperty("detail").GetString(),
            (await ReadAsync(unknownEmail)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task The_cart_needs_a_signed_in_user()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync("/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_refresh_token_comes_only_in_an_httponly_cookie_that_the_browser_sends_back()
    {
        using var factory = new ApiFactory();
        var browser = factory.CreateClient();   // keeps cookies like a browser

        var registered = await browser.PostAsJsonAsync("/auth/register",
            new { name = "Mona Hassan", email = "mona@example.com", password = Password });
        Assert.False((await ReadAsync(registered)).TryGetProperty("refreshToken", out _));

        var cookie = registered.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshCookie));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/auth", cookie, StringComparison.OrdinalIgnoreCase);

        var refreshed = await browser.PostAsync("/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal("mona@example.com", (await ReadAsync(refreshed)).GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Refresh_without_a_cookie_is_refused()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().PostAsync("/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_and_reusing_an_old_token_ends_every_session()
    {
        using var factory = new ApiFactory();
        var client = CookielessClient(factory);
        var first = (await RegisterAsync(client, "mona@example.com")).Refresh;

        // A normal refresh returns a new pair
        var rotated = await PostWithCookieAsync(client, "/auth/refresh", first);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var second = RefreshCookieOf(rotated);
        Assert.NotEqual(first, second);

        // The first token again means someone kept a copy, so it is refused
        var reused = await PostWithCookieAsync(client, "/auth/refresh", first);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);

        // and the newer token is revoked with it
        var afterReuse = await PostWithCookieAsync(client, "/auth/refresh", second);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_ends_that_session_only()
    {
        using var factory = new ApiFactory();
        var client = CookielessClient(factory);
        var laptop = await RegisterAsync(client, "mona@example.com");
        var phone = await LoginAsync(client, "mona@example.com", Password);

        var logout = await PostWithCookieAsync(client, "/auth/logout", laptop.Refresh);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), c => c.StartsWith($"{RefreshCookie}=;"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostWithCookieAsync(client, "/auth/refresh", laptop.Refresh)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostWithCookieAsync(client, "/auth/refresh", phone.Refresh)).StatusCode);
    }

    [Fact]
    public async Task Only_an_admin_can_add_a_product()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var product = new
        {
            name = "XX99 Mark I Headphones",
            description = "As the gold standard for headphones, the classic XX99 Mark I offers detailed audio.",
            price = 9_999m,
            stock = 12,
            category = "Headphones",
            inTheBox = new Dictionary<string, int> { ["Headphone unit"] = 1, ["Replacement earcups"] = 2 },
        };

        Authorize(client, (await RegisterAsync(client, "mona@example.com")).Access);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/admin/products", product)).StatusCode);

        Authorize(client, (await LoginAsync(client, ApiFactory.AdminEmail, ApiFactory.AdminPassword)).Access);
        var created = await client.PostAsJsonAsync("/admin/products", product);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var body = await ReadAsync(created);
        Assert.Equal(2, body.GetProperty("inTheBox").GetProperty("Replacement earcups").GetInt32());
    }

    [Fact]
    public async Task A_customer_cannot_read_another_customers_address()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        Authorize(client, (await RegisterAsync(client, "mona@example.com")).Access);
        var created = await client.PostAsJsonAsync("/addresses", new
        {
            fullAddressName = "Mona Hassan", street = "12 Tahrir Street", city = "Cairo",
            country = "Egypt", phone = "+201000000000", postalCode = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var addressId = (await ReadAsync(created)).GetProperty("id").GetString();

        Authorize(client, (await RegisterAsync(client, "omar@example.com")).Access);
        var response = await client.GetAsync($"/addresses/{addressId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);   // not 403: it does not even exist for Omar
    }

    // Refresh is the cookie as a browser would send it back, name=value
    private static async Task<(string Access, string Refresh)> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { name = "Test Customer", email, password = Password });
        response.EnsureSuccessStatusCode();
        return ((await ReadAsync(response)).GetProperty("accessToken").GetString()!, RefreshCookieOf(response));
    }

    private static async Task<(string Access, string Refresh)> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return ((await ReadAsync(response)).GetProperty("accessToken").GetString()!, RefreshCookieOf(response));
    }

    // Does not keep cookies, so each test picks which refresh token goes with a request
    private static HttpClient CookielessClient(ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshCookie)).Split(';')[0];

    private static Task<HttpResponseMessage> PostWithCookieAsync(HttpClient client, string path, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    private static void Authorize(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
