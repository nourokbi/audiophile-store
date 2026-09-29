namespace Audiophile.Application.Common;

/// <summary>
/// The role names Identity stores in <c>AspNetRoles</c>. Constants rather than
/// literals so the <c>[Authorize]</c> attributes, the seeder and the JWT builder
/// cannot drift apart over a typo.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    /// <summary>Every role the seeder must ensure exists.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Customer];
}
