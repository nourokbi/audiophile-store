using Audiophile.Application.Abstractions;
using Audiophile.Application.Payments;
using Audiophile.Infrastructure.Payments;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Audiophile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Audiophile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(configuration.GetConnectionString("Sqlite") ?? "Data Source=audiophile.db"));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<FawaterakOptions>(configuration.GetSection(FawaterakOptions.SectionName));

        services.AddHttpClient<FawaterakGateway>((sp, client) =>
                client.BaseAddress = new Uri(
                    sp.GetRequiredService<IOptions<FawaterakOptions>>().Value.BaseUrl.TrimEnd('/') + "/"))
            .AddStandardResilienceHandler(ConfigureGatewayResilience);

        // Each gateway is reachable by name; the resolver picks the default for new checkouts.
        // A second gateway is one more AddHttpClient + AddKeyedTransient pair.
        services.AddKeyedTransient<IPaymentGateway>("fawaterak", (sp, _) => sp.GetRequiredService<FawaterakGateway>());
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

        services.AddHostedService<PaymentExpiryWorker>();
        return services;
    }

    private static void ConfigureGatewayResilience(HttpStandardResilienceOptions o)
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);

        // Retries only for GET (status lookups). Creating a payment, refunding or
        // voiding is never repeated automatically: after a timeout we cannot know
        // whether the first attempt went through.
        o.Retry.DisableForUnsafeHttpMethods();

        // Stop calling a gateway for a while if half the recent calls fail, so an
        // outage fails fast instead of tying up every request thread.
        o.CircuitBreaker.FailureRatio = 0.5;
        o.CircuitBreaker.MinimumThroughput = 10;
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    }
}
