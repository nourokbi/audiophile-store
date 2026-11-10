using System.Text.Json.Serialization;
using Audiophile.API.Auth;
using Audiophile.API.ExceptionHandling;
using Audiophile.API.RateLimiting;
using Audiophile.Application;
using Audiophile.Application.Abstractions;
using Audiophile.Infrastructure;
using Audiophile.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Readable lines while developing, one JSON object per line elsewhere so the host's log search can filter on each field
builder.Services.AddSerilog((services, logger) =>
{
    logger.ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "audiophile-api");

    if (builder.Environment.IsDevelopment())
        logger.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    else
        logger.WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddRateLimitPolicies();

// The allowed shop origins come from configuration, so production adds its domain without a code change
builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Trusts the hosting proxy's forwarded headers so the rate limiter sees the client's address
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Applies migrations when configured and creates the admin account
await app.Services.InitialiseDatabaseAsync();

app.UseForwardedHeaders();

// One line per request with its status and time, health checks only show when they fail
app.UseSerilogRequestLogging(o =>
{
    o.GetLevel = (http, _, ex) =>
        ex is not null || http.Response.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error
        : http.Request.Path.StartsWithSegments("/health") ? Serilog.Events.LogEventLevel.Verbose
        : Serilog.Events.LogEventLevel.Information;
    o.EnrichDiagnosticContext = (diagnostics, http) =>
    {
        diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        if (http.User.FindFirst("sub")?.Value is { } userId)
            diagnostics.Set("UserId", userId);
    };
});

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No HTTPS redirect because the ngrok tunnel forwards to plain http and a redirect would break the webhook

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
