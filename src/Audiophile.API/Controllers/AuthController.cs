using Audiophile.API.RateLimiting;
using Audiophile.Application.Abstractions;
using Audiophile.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Audiophile.API.Controllers;

// Sign up, sign in, keep the session alive and sign out
// The refresh token travels only in an httpOnly cookie, so page scripts can never read it
[ApiController]
[Route("auth")]
public sealed class AuthController(AuthService auth, ICurrentUser currentUser) : ControllerBase
{
    private const string RefreshCookie = "audiophile_refresh";

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var session = StartSession(await auth.RegisterAsync(request, ct));
        return CreatedAtAction(nameof(Me), null, session);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Ok(StartSession(await auth.LoginAsync(request, ct)));

    // Trades the cookie's refresh token for a new pair and the old one stops working, the shop calls it on every page load
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    public async Task<IActionResult> Refresh(CancellationToken ct) =>
        Ok(StartSession(await auth.RefreshAsync(Request.Cookies[RefreshCookie], ct)));

    // Ends the session in the cookie, works even after the access token expired
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(Request.Cookies[RefreshCookie], ct);
        Response.Cookies.Delete(RefreshCookie, CookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await auth.GetCurrentUserAsync(currentUser.Id, ct));

    private SessionDto StartSession(AuthResponse response)
    {
        var options = CookieOptions();
        options.Expires = response.RefreshTokenExpiresAt;
        Response.Cookies.Append(RefreshCookie, response.RefreshToken, options);

        return new SessionDto(response.AccessToken, response.AccessTokenExpiresAt, response.User);
    }

    // Sent only to the auth endpoints and never from another site, Secure follows the request so local http still works
    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/auth",
    };
}
