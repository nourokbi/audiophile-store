using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Audiophile.API.RateLimiting;

// Per address limits on the login and register endpoints, kept in memory for now
public static class RateLimitPolicies
{
    public const string Login = "login";        // ten a minute
    public const string Refresh = "refresh";    // thirty a minute, every page load restores the session with one
    public const string Register = "register";  // five new accounts an hour

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Login, http => PerIp(http, 10, TimeSpan.FromMinutes(1)));
            o.AddPolicy(Refresh, http => PerIp(http, 30, TimeSpan.FromMinutes(1)));
            o.AddPolicy(Register, http => PerIp(http, 5, TimeSpan.FromHours(1)));
        });

    private static RateLimitPartition<string> PerIp(HttpContext http, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window });
}
