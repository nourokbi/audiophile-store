using Audiophile.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

// Seeds the Admin and Customer roles into Identity's roles table
internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> b)
    {
        b.HasData(
            Role("1b6c8f43-4d1e-4f0a-9a7e-1d1b0a000001", Roles.Admin),
            Role("1b6c8f43-4d1e-4f0a-9a7e-1d1b0a000002", Roles.Customer));
    }

    // A seed needs a fixed concurrency stamp instead of a random one
    private static IdentityRole<Guid> Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}
