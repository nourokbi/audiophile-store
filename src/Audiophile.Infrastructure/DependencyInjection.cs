using Audiophile.Application.Abstractions;
// using Audiophile.Application.Payments;           // PAYMENT GATEWAY (off)
using Audiophile.Domain.Entities;
using Audiophile.Infrastructure.Auth;
// using Audiophile.Infrastructure.Payments;        // PAYMENT GATEWAY (off)
// using Audiophile.Infrastructure.Payments.Fawaterak;  // PAYMENT GATEWAY (off)
using Audiophile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Http.Resilience;      // PAYMENT GATEWAY (off)
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Audiophile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL everywhere. Locally: the docker-compose container (appsettings.Development.json).
        // In production: Neon, through the ConnectionStrings__Default environment variable.
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        AddIdentityAndJwt(services, configuration);

        // PAYMENT GATEWAY (off): Fawaterak client and the expiry job are not registered.
        // See the note at the top of Application/Orders/OrderService.cs.
        // services.Configure<FawaterakOptions>(configuration.GetSection(FawaterakOptions.SectionName));
        //
        // // Fawaterak is the only gateway: OrderService and FawaterakController ask for
        // // IFawaterakGateway and get FawaterakGateway with a typed HttpClient.
        // services.AddHttpClient<IFawaterakGateway, FawaterakGateway>((sp, client) =>
        //         client.BaseAddress = new Uri(
        //             sp.GetRequiredService<IOptions<FawaterakOptions>>().Value.BaseUrl.TrimEnd('/') + "/"))
        //     .AddStandardResilienceHandler(ConfigureGatewayResilience);
        //
        // services.AddHostedService<PaymentExpiryWorker>();
        return services;
    }

    private static void AddIdentityAndJwt(IServiceCollection services, IConfiguration configuration)
    {
        // Identity without its cookie sign-in: UserManager for passwords, lockout and roles.
        services.AddIdentityCore<User>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        // Fails at startup, not at the first login, when the signing key is missing or weak.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.Key.Length >= JwtOptions.MinKeyLength,
                $"Jwt:Key must be at least {JwtOptions.MinKeyLength} characters. Set it with dotnet user-secrets or the Jwt__Key environment variable.")
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep the claim names exactly as JwtTokenService wrote them ("sub", "role").
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = JwtTokenService.SigningKey(jwt),
                    ClockSkew = TimeSpan.Zero,      // a 15-minute token means 15 minutes, not 20
                    NameClaimType = "name",
                    RoleClaimType = JwtTokenService.RoleClaim,   // what [Authorize(Roles = ...)] checks
                };
            });
        services.AddAuthorization();
    }

    // PAYMENT GATEWAY (off)
    // private static void ConfigureGatewayResilience(HttpStandardResilienceOptions o)
    // {
    //     o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
    //     o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
    //
    //     // Retries only for GET (invoice lookups). Creating an invoice is never
    //     // repeated automatically: after a timeout we cannot know
    //     // whether the first attempt went through.
    //     o.Retry.DisableForUnsafeHttpMethods();
    //
    //     // Stop calling Fawaterak for a while if half the recent calls fail, so an
    //     // outage fails fast instead of tying up every request thread.
    //     o.CircuitBreaker.FailureRatio = 0.5;
    //     o.CircuitBreaker.MinimumThroughput = 10;
    //     o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    //     o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    // }
}
