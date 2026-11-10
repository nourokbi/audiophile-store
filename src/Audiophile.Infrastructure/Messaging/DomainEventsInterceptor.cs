using Audiophile.Domain.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Infrastructure.Messaging;

// Publishes the events the saved entities raised, the bus outbox writes them in the same transaction as the entities
internal sealed class DomainEventsInterceptor(IServiceProvider services) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
            await PublishAsync(context, cancellationToken);
        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
            PublishAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    private async Task PublishAsync(DbContext context, CancellationToken ct)
    {
        var events = context.ChangeTracker.Entries<HasDomainEvents>()
            .SelectMany(entry => entry.Entity.TakeDomainEvents())
            .ToList();
        if (events.Count == 0)
            return;

        // Resolved here and not in the constructor because the outbox publisher itself needs this DbContext
        var publisher = services.GetRequiredService<IPublishEndpoint>();
        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, domainEvent.GetType(), ct);
    }
}
