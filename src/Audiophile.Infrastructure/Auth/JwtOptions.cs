namespace Audiophile.Infrastructure.Auth;

// The "Jwt" section. Key is a secret: user-secrets locally, an environment
// variable (Jwt__Key) in production. Never appsettings.json.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits; anything shorter fails at sign time.
    public const int MinKeyLength = 32;

    public string Issuer { get; init; } = "audiophile-api";
    public string Audience { get; init; } = "audiophile-client";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;
}
