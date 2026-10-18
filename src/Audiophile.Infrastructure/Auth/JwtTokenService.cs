using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Audiophile.Application.Abstractions;
using Audiophile.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Audiophile.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    // The API reads the short claim names back unchanged because MapInboundClaims is off
    public const string RoleClaim = "role";

    public AccessToken CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(RoleClaim, r)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(SigningKey(jwt), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expires);
    }

    public NewRefreshToken CreateRefreshToken()
    {
        // Random bytes encoded URL safe, so the token can travel in a cookie later
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        return new NewRefreshToken(token, HashRefreshToken(token), DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays));
    }

    // A plain SHA256 is enough because a long random token cannot be guessed
    public string HashRefreshToken(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    internal static SymmetricSecurityKey SigningKey(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.Key));
}
