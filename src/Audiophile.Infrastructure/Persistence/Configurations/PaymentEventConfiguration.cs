using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> b)
    {
        b.Property(e => e.Provider).HasMaxLength(50);
        b.Property(e => e.ExternalId).HasMaxLength(255);
        b.Property(e => e.EventType).HasMaxLength(100);

        // M4-11 / M5-05: the database, not an if-statement, guarantees each gateway
        // event is applied at most once, even when two deliveries arrive together.
        b.HasIndex(e => new { e.Provider, e.ExternalId }).IsUnique();
        b.HasIndex(e => e.PaymentId);
    }
}
