using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Audiophile.Application.Abstractions;

// What the services need from the database, implemented by AppDbContext in Infrastructure
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
    DbSet<AdminNotification> AdminNotifications { get; }

    // For the few places that need an explicit transaction
    DatabaseFacade Database { get; }

    // Forgets the loaded rows before a retry so the second attempt reads fresh ones
    void ClearForRetry();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
