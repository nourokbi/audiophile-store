using System.Text.Json.Serialization;
using Audiophile.API.ExceptionHandling;
using Audiophile.Application;
using Audiophile.Infrastructure;
using Audiophile.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

app.UseExceptionHandler();

// The demo storefront (wwwroot): "/" is the shop, "/orders.html" shows order status.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No HTTPS redirect: in development the gateway reaches us through an HTTPS tunnel
// that forwards to plain http://localhost, and a redirect would break the webhook.

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
