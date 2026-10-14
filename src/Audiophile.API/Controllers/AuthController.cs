using Audiophile.API.RateLimiting;
using Audiophile.Application.Abstractions;
using Audiophile.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Audiophile.API.Controllers;

// Sign up, sign in, keep the session alive, sign out.
// The access token goes in "Authorization: Bearer <token>"; the refresh token is
// only ever sent to /auth/refresh and /auth/logout.
[ApiController]
[Route("auth")]
public sealed class AuthController(AuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var session = await auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Me), null, session);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    // Trades a refresh token for a new access token AND a new refresh token; the old one stops working.
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(request, ct));

    // Only ever ends the caller's own session.
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(currentUser.Id, request, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await auth.GetCurrentUserAsync(currentUser.Id, ct));
}
