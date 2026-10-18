using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

// Identity maps the account columns, this adds the shop's own ones
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Name).HasMaxLength(User.NameMaxLength);

        // Exactly one cart per user
        b.HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
