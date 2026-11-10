using System.Collections.Concurrent;
using Audiophile.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Audiophile.Tests.Api;

// Runs the real API in memory against a fresh test database
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "admin-password-1";

    private readonly TestDatabase _database = new();

    // Every email the API sends, instead of sending it
    public SentEmails Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);
        builder.UseSetting("Jwt:Key", new string('k', 48));
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Emails));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _database.Dispose();
    }
}

// Keeps the emails, the bus delivers them a moment after the request so a test waits for the one it expects
public sealed class SentEmails : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> All => _sent.ToList();

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public async Task<EmailMessage> WaitForAsync(Func<EmailMessage, bool> match)
    {
        for (var waited = 0; waited < 20_000; waited += 100)
        {
            if (_sent.FirstOrDefault(match) is { } email)
                return email;
            await Task.Delay(100);
        }
        throw new TimeoutException($"No matching email was sent, sent: {string.Join(" | ", _sent.Select(e => e.Subject))}");
    }
}
