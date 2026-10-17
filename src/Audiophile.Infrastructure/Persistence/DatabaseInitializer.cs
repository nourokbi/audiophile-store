using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Audiophile.Infrastructure.Persistence;

// Runs once at startup. Fixed data (roles, sample products) is seeded by HasData in
// the entity configurations; this only does the two things HasData cannot:
//   1. apply migrations, when "Database:MigrateOnStartup" is true (local development);
//   2. create the admin account, because its password is a secret from configuration
//      and has to be hashed by Identity, so it cannot be written into a migration.
public static class DatabaseInitializer
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
            logger.LogInformation("Database migrations applied");
        }

        await EnsureAdminAsync(sp.GetRequiredService<UserManager<User>>(), configuration, logger);
    }

    private static async Task EnsureAdminAsync(UserManager<User> users, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin:Email / Admin:Password not set; no admin account created");
            return;
        }

        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = User.Create(configuration["Admin:Name"] ?? "Store Admin", email);
            Check(await users.CreateAsync(admin, password), "create the admin account");
            logger.LogInformation("Admin account {Email} created", email);
        }

        if (!await users.IsInRoleAsync(admin, Roles.Admin))
            Check(await users.AddToRoleAsync(admin, Roles.Admin), "give the admin account the Admin role");
    }

    // Fail loudly at startup: a misconfigured admin should stop the app, not be discovered later.
    private static void Check(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Could not {action}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
    }
}
