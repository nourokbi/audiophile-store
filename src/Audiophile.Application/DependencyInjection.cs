using Audiophile.Application.Orders;
using Audiophile.Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentResultApplier>();
        return services;
    }
}
