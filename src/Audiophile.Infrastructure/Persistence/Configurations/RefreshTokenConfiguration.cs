using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        // The id is made in the constructor, so EF must insert a new token instead of updating it
        b.Property(t => t.Id).ValueGeneratedNever();

        // Holds the hash of the token, never the token itself
        b.Property(t => t.Token).HasMaxLength(128);
        b.HasIndex(t => t.Token).IsUnique();

        b.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
