using Audiophile.Application.Addresses;
using Audiophile.Application.Auth;
using Audiophile.Application.Carts;
using Audiophile.Application.Orders;
using Audiophile.Application.Products;
using Audiophile.Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<AddressService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentResultApplier>();
        return services;
    }
}
