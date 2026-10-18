using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Audiophile.Tests.Api;

// Boots the real API in memory: the same Program.cs, controllers, auth and services.
// Only the connection string changes: it points at a fresh PostgreSQL database for this
// test (TestDatabase), already migrated, so startup can create the admin account in it.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "admin-password-1";

    private readonly TestDatabase _database = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);
        builder.UseSetting("Jwt:Key", new string('k', 48));
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);        // stop the app first, so nothing is still connected
        if (disposing)
            _database.Dispose();
    }
}
