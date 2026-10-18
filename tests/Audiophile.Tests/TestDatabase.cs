using Audiophile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Audiophile.Tests;

// A real PostgreSQL database for one test: created from the real migrations, deleted when
// the test ends. Every test gets its own, so tests can run in parallel without seeing
// each other's rows. PostgreSQL (not SQLite) because the app relies on PostgreSQL-only
// features: xmin for stock concurrency, jsonb for "in the box".
//
// The server comes from, first match wins:
//   1. ConnectionStrings__Tests environment variable (CI)
//   2. ConnectionStrings:Tests in the API's user-secrets
//   3. ConnectionStrings:Default in the API's user-secrets (your local PostgreSQL)
// Only the server and login are used; the database name is always a new one.
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
        db.Database.Migrate();          // creates the database, the tables and the seed data
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
