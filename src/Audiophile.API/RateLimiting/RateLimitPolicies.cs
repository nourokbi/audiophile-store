using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Audiophile.API.RateLimiting;

// Limits on the endpoints worth guessing at. Counted per client IP, which is only the
// real IP because UseForwardedHeaders runs first (see Program.cs).
// In memory for now: with two or more API instances this moves to Redis (roadmap Phase 6).
public static class RateLimitPolicies
{
    public const string Login = "login";        // login and refresh: 10 a minute
    public const string Register = "register";  // new accounts: 5 an hour

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Login, http => PerIp(http, 10, TimeSpan.FromMinutes(1)));
            o.AddPolicy(Register, http => PerIp(http, 5, TimeSpan.FromHours(1)));
        });

    private static RateLimitPartition<string> PerIp(HttpContext http, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window });
}
