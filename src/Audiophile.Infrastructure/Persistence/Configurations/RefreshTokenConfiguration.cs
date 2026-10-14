using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        // Added through User.IssueRefreshToken to an already-saved user; see AddressConfiguration.
        b.Property(t => t.Id).ValueGeneratedNever();

        // Holds the SHA-256 hash of the token, never the token itself.
        b.Property(t => t.Token).HasMaxLength(128);
        b.HasIndex(t => t.Token).IsUnique();

        b.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
