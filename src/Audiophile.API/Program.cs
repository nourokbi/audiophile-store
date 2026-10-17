using System.Text.Json.Serialization;
using Audiophile.API.Auth;
using Audiophile.API.ExceptionHandling;
using Audiophile.API.RateLimiting;
using Audiophile.Application;
using Audiophile.Application.Abstractions;
using Audiophile.Infrastructure;
using Audiophile.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);   // database, Identity, JWT

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddRateLimitPolicies();

// The React client runs on another origin (Vite: http://localhost:5173). Origins come
// from configuration, so production lists the real domain without a code change.
builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Hosting platforms put a proxy in front of the app. Trusting its X-Forwarded-For is what
// gives the rate limiter the client's IP rather than the proxy's (M1-11).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Applies migrations when Database:MigrateOnStartup is true, and creates the admin account.
await app.Services.InitialiseDatabaseAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No HTTPS redirect: in development the gateway reaches us through an HTTPS tunnel
// that forwards to plain http://localhost, and a redirect would break the webhook.

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
