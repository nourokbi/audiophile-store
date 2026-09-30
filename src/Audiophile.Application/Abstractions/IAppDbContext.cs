using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Abstractions;

/// <summary>
/// What the use cases need from persistence. Infrastructure's AppDbContext
/// implements it, so Application never references Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentEvent> PaymentEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
