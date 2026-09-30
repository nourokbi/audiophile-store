namespace Audiophile.Application.Common;

// Authentication is out of scope for the payment slice, so every request acts as
// this seeded customer. Replace with the JWT user id once Phase 3 lands.
public static class DemoUser
{
    public static readonly Guid Id = Guid.Parse("7c1f2b5e-0000-4000-8000-000000000001");
    public const string Name = "Demo Customer";
    public const string Email = "demo.customer@example.com";
}
