using Audiophile.Application.Abstractions;
using Audiophile.Application.Notifications;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Audiophile.Infrastructure.Auth;
using Audiophile.Infrastructure.Email;
using Audiophile.Infrastructure.Messaging;
using Audiophile.Infrastructure.Payments;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Audiophile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Audiophile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL locally and in production, read from ConnectionStrings Default
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>((sp, o) => o
            .UseNpgsql(connectionString)
            .AddInterceptors(sp.GetRequiredService<DomainEventsInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        AddIdentityAndJwt(services, configuration);

        services.Configure<FawaterakOptions>(configuration.GetSection(FawaterakOptions.SectionName));

        // A typed HttpClient pointed at Fawaterak's API, with timeouts and retries for lookups
        services.AddHttpClient<IFawaterakGateway, FawaterakGateway>((sp, client) =>
                client.BaseAddress = new Uri(
                    sp.GetRequiredService<IOptions<FawaterakOptions>>().Value.BaseUrl.TrimEnd('/') + "/"))
            .AddStandardResilienceHandler(ConfigureGatewayResilience);

        // Cancels unpaid card orders every five minutes
        services.AddHostedService<PaymentExpiryWorker>();

        services.AddMessaging(configuration);
        AddEmail(services, configuration);
        return services;
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShopOptions>(configuration.GetSection(ShopOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var provider = configuration[$"{EmailOptions.SectionName}:Provider"];
        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else if (string.Equals(provider, "Brevo", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        else
            services.AddScoped<IEmailSender, LogEmailSender>();
    }

    private static void AddIdentityAndJwt(IServiceCollection services, IConfiguration configuration)
    {
        // Identity without its cookie sign in, used for passwords, lockout and roles
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

        // Fails at startup when the signing key is missing or too short
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

                // Keep the claim names exactly as JwtTokenService wrote them
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = JwtTokenService.SigningKey(jwt),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = JwtTokenService.RoleClaim,
                };
            });
        services.AddAuthorization();
    }

    private static void ConfigureGatewayResilience(HttpStandardResilienceOptions o)
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);

        // Only lookups are retried, creating an invoice twice could charge the customer twice
        o.Retry.DisableForUnsafeHttpMethods();

        // Polly requires the circuit breaker window to be at least twice the attempt timeout
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    }
}
