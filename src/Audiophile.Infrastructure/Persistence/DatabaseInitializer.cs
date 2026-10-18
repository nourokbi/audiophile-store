using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Audiophile.Infrastructure.Persistence;

// Runs at startup, applies migrations when Database:MigrateOnStartup is true and creates the admin account from configuration
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

    // A misconfigured admin stops the app at startup instead of being discovered later
    private static void Check(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Could not {action}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
    }
}
