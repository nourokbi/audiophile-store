using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        // The id is made in the constructor, so EF must insert a new address instead of updating it
        b.Property(a => a.Id).ValueGeneratedNever();

        // The same limits the Address entity checks
        b.Property(a => a.FullAddressName).HasMaxLength(100);
        b.Property(a => a.Street).HasMaxLength(200);
        b.Property(a => a.City).HasMaxLength(100);
        b.Property(a => a.Country).HasMaxLength(100);
        b.Property(a => a.Phone).HasMaxLength(20);
        b.Property(a => a.PostalCode).HasMaxLength(20);

        b.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(a => new { a.UserId, a.IsActive });
    }
}
