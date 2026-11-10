using Audiophile.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Infrastructure.Messaging;

internal static class MessagingSetup
{
    // RabbitMQ when ConnectionStrings RabbitMq is set, such as amqp://user:password@rabbitmq:5672, otherwise an in memory
    // bus inside the API, so dotnet run and the tests work without RabbitMQ
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMq = configuration.GetConnectionString("RabbitMq");

        services.AddScoped<DomainEventsInterceptor>();
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<CustomerEmailConsumer>();
            bus.AddConsumer<AdminNotificationConsumer>();

            // Published events are saved in the database with the change that raised them, then a background service sends them
            bus.AddEntityFrameworkOutbox<AppDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.DuplicateDetectionWindow = TimeSpan.FromDays(1);
            });

            // A failed message is tried again later, and the inbox in the database makes a repeated message run once
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)));
                endpoint.UseEntityFrameworkOutbox<AppDbContext>(context);
            });

            if (string.IsNullOrWhiteSpace(rabbitMq))
                bus.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
            else
                bus.UsingRabbitMq((context, transport) =>
                {
                    transport.Host(new Uri(rabbitMq));
                    transport.ConfigureEndpoints(context);
                });
        });

        return services;
    }
}
