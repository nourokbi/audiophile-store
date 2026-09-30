namespace Audiophile.Application.Common;

// The role names Identity stores in AspNetRoles. Constants rather than
// literals so the [Authorize] attributes, the seeder and the JWT builder
// cannot drift apart over a typo.
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    // Every role the seeder must ensure exists.
    public static readonly IReadOnlyList<string> All = [Admin, Customer];
}
