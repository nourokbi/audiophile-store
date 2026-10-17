using Audiophile.Domain.Entities;

namespace Audiophile.Application.Abstractions;

// Issues the two tokens a session is made of. Infrastructure implements it with
// the signing key from configuration.
public interface ITokenService
{
    // Short-lived JWT the client sends as "Authorization: Bearer ...".
    AccessToken CreateAccessToken(User user, IEnumerable<string> roles);

    // Long-lived random string the client trades for a new pair. Only its hash is stored,
    // so a leaked database cannot be used to sign in.
    NewRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed record NewRefreshToken(string Token, string Hash, DateTime ExpiresAt);
