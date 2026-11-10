namespace Audiophile.Domain.Common;

// Something that happened to an entity, saved in the same transaction as the entity and then sent to the message bus
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

// An entity that keeps what happened to it until the database context takes the events on the next save
public abstract class HasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
