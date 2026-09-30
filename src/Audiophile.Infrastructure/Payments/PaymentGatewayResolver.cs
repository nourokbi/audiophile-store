using Audiophile.Application.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Infrastructure.Payments;

/// <summary>
/// Gateways are registered as keyed services under their names ("paymob",
/// "fawaterak"). "Payments:Provider" in configuration picks the default.
/// </summary>
public sealed class PaymentGatewayResolver(IServiceProvider services, IConfiguration configuration)
    : IPaymentGatewayResolver
{
    public static readonly string[] Supported = ["fawaterak"];

    public IPaymentGateway Default => Get(configuration["Payments:Provider"] ?? "fawaterak");

    public IPaymentGateway Get(string name)
    {
        var key = name.Trim().ToLowerInvariant();
        if (!Supported.Contains(key))
            throw new InvalidOperationException($"Unknown payment gateway '{name}'.");

        return services.GetRequiredKeyedService<IPaymentGateway>(key);
    }
}
