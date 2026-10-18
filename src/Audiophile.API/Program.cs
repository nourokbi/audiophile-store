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
