using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Audiophile.Application.Abstractions;

// What the use cases need from persistence. Infrastructure's AppDbContext
// implements it, so Application never references Infrastructure.
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Address> Addresses { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Product> Products { get; }
    DbSet<Cart> Carts { get; }
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentEvent> PaymentEvents { get; }

    // For the few use cases that need an explicit transaction (e.g. create a user and give it a role).
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
