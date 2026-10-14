using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        // Made in the constructor and added through User.AddAddress: without this, EF sees a
        // key that is already set on an existing user's new address and tries an UPDATE.
        b.Property(a => a.Id).ValueGeneratedNever();

        // The same limits Address enforces with Guard.
        b.Property(a => a.FullAddressName).HasMaxLength(100);
        b.Property(a => a.Street).HasMaxLength(200);
        b.Property(a => a.City).HasMaxLength(100);
        b.Property(a => a.Country).HasMaxLength(100);
        b.Property(a => a.Phone).HasMaxLength(20);
        b.Property(a => a.PostalCode).HasMaxLength(20);

        b.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);   // user deleted → their addresses go too

        b.HasIndex(a => new { a.UserId, a.IsActive });
    }
}
