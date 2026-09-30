using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Infrastructure.Persistence;

public static class DbSeeder
{
    // Creates the SQLite database and fills it with a demo customer and catalogue.
    // EnsureCreated is a demo shortcut: the real project uses migrations (M6-05).
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Users.AnyAsync(u => u.Id == DemoUser.Id, ct))
        {
            var user = User.Create(DemoUser.Name, DemoUser.Email);
            user.Id = DemoUser.Id;                       // fixed id so requests can act as this user
            user.NormalizedEmail = DemoUser.Email.ToUpperInvariant();
            user.NormalizedUserName = user.NormalizedEmail;
            user.SecurityStamp = Guid.NewGuid().ToString();
            db.Users.Add(user);
        }

        if (!await db.Products.AnyAsync(ct))
        {
            db.Products.AddRange(
                new Product("XX99 Mark II Headphones",
                    "The new XX99 Mark II headphones is the pinnacle of pristine audio.",
                    price: 14_999m, stock: 25, ProductCategory.Headphones),
                new Product("XX59 Headphones",
                    "Enjoy your audio almost anywhere and customize it to your specific tastes.",
                    price: 4_499m, stock: 40, ProductCategory.Headphones),
                new Product("ZX9 Speaker",
                    "Upgrade your sound system with the all new ZX9 active speaker.",
                    price: 22_500m, stock: 10, ProductCategory.Speakers),
                new Product("YX1 Wireless Earphones",
                    "Tailor your listening experience with bespoke dynamic drivers.",
                    price: 2_999m, stock: 3, ProductCategory.Earphones));
        }

        await db.SaveChangesAsync(ct);
    }
}
