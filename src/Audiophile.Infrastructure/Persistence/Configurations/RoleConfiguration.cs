using Audiophile.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

// Identity's AspNetRoles table. The roles are fixed data, so they are seeded here
// and exist from the first migration on.
internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> b)
    {
        b.HasData(
            Role("1b6c8f43-4d1e-4f0a-9a7e-1d1b0a000001", Roles.Admin),
            Role("1b6c8f43-4d1e-4f0a-9a7e-1d1b0a000002", Roles.Customer));
    }

    // The concurrency stamp is normally random; a seed has to be the same every build.
    private static IdentityRole<Guid> Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}
