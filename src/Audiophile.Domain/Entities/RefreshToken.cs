using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

/// <summary>
/// One issued refresh token. Part of the <see cref="User"/> aggregate — issued
/// by <see cref="User.IssueRefreshToken"/> so a token can never end up attached
/// to nobody.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken() { } // EF Core

    internal RefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        UserId = Guard.NotEmpty(userId, nameof(userId));
        Token = Guard.Text(token, 16, 512, nameof(token));

        var expiry = Guard.Utc(expiresAt);
        if (expiry <= DateTime.UtcNow)
            throw new ArgumentException("A refresh token must expire in the future.", nameof(expiresAt));

        ExpiresAt = expiry;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Token { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>The only question callers should ask. Revoked and expired are one answer, not two checks to remember.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>Idempotent: revoking twice is not an error, it is the same outcome.</summary>
    public void Revoke()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
    }
}
