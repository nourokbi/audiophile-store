namespace Audiophile.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Name, string Email, IReadOnlyList<string> Roles);

// What the service hands the API, the refresh token then goes into an httpOnly cookie and never into the body
public sealed record AuthResponse(
    string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, UserDto User);

// What the browser gets back, the access token lives in memory only
public sealed record SessionDto(string AccessToken, DateTime AccessTokenExpiresAt, UserDto User);
