using Audiophile.Domain.Entities;

namespace Audiophile.Application.Abstractions;

// Issues the access token and refresh token of a session
public interface ITokenService
{
    // A short lived JWT the client sends in the Authorization header
    AccessToken CreateAccessToken(User user, IEnumerable<string> roles);

    // A long lived random string the client trades for a new pair, only its hash is stored
    NewRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed record NewRefreshToken(string Token, string Hash, DateTime ExpiresAt);
