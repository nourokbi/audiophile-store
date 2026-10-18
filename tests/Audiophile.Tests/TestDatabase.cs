using Audiophile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Audiophile.Tests;

// A new PostgreSQL database per test built from the real migrations and deleted afterwards, the server comes from ConnectionStrings Tests or Default
public sealed class TestDatabase : IDisposable
{
    private static readonly string ServerConnectionString = FindServer();

    public TestDatabase()
    {
        ConnectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString)
        {
            Database = $"audiophile_test_{Guid.NewGuid():N}",
        }.ConnectionString;

        using var db = NewContext();
        db.Database.Migrate();
    }

    public string ConnectionString { get; }

    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public void Dispose()
    {
        using var db = NewContext();
        db.Database.EnsureDeleted();
    }

    private static string FindServer()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        return config.GetConnectionString("Tests")
               ?? config.GetConnectionString("Default")
               ?? throw new InvalidOperationException(
                   "No PostgreSQL for the tests. Set ConnectionStrings:Default in the API's user-secrets " +
                   "(see docs/database.md) or the ConnectionStrings__Tests environment variable.");
    }
}
