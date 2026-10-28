using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Auth;

// Register, log in, refresh and log out, Identity's UserManager owns passwords and roles and this class owns the tokens
public sealed class AuthService(
    UserManager<User> users,
    IAppDbContext db,
    ITokenService tokens,
    ILogger<AuthService> logger)
{
    // One message for an unknown email and a wrong password, so nobody can find out who has an account
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidSession = "Your session has expired. Please log in again.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = User.Create(request.Name, request.Email);

        // Creating the user and giving it a role happen in one transaction, so no account is left without a role
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                throw new ConflictException("An account with this email already exists.");

            // Identity's password rule messages are written for people to read
            throw new ArgumentException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        var roled = await users.AddToRoleAsync(user, Roles.Customer);
        if (!roled.Succeeded)
            throw new ApplicationException($"Could not give the new user the {Roles.Customer} role.");

        var response = await StartSessionAsync(user, [Roles.Customer], ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || await users.IsLockedOutAsync(user))
            throw new UnauthorizedException(InvalidCredentials);

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            // Five wrong passwords lock the account for five minutes
            await users.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentials);
        }

        await users.ResetAccessFailedCountAsync(user);
        return await StartSessionAsync(user, await users.GetRolesAsync(user), ct);
    }

    // Every refresh token works once, trading it in revokes it and issues a new pair
    public async Task<AuthResponse> RefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedException(InvalidSession);

        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == hash, ct)
            ?? throw new UnauthorizedException(InvalidSession);

        if (stored.IsRevoked)
        {
            // A revoked token used again means someone copied it, so every session of this user is ended
            var active = await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var token in active)
                token.Revoke();
            await db.SaveChangesAsync(ct);

            logger.LogWarning("Refresh token reuse for user {UserId}; all sessions revoked", stored.UserId);
            throw new UnauthorizedException(InvalidSession);
        }

        if (stored.IsExpired)
            throw new UnauthorizedException(InvalidSession);

        stored.Revoke();
        return await StartSessionAsync(stored.User, await users.GetRolesAsync(stored.User), ct);
    }

    // Deletes the token instead of revoking it, so a later reuse is not mistaken for theft
    // Holding the token is the proof of the session, so no access token is needed and an expired one still logs out
    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hash, ct);
        if (stored is null)
            return;

        db.RefreshTokens.Remove(stored);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("Your account no longer exists.");
        return ToDto(user, await users.GetRolesAsync(user));
    }

    private async Task<AuthResponse> StartSessionAsync(User user, IList<string> roles, CancellationToken ct)
    {
        var access = tokens.CreateAccessToken(user, roles);
        var refresh = tokens.CreateRefreshToken();
        user.IssueRefreshToken(refresh.Hash, refresh.ExpiresAt);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(access.Token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt, ToDto(user, roles));
    }

    private static UserDto ToDto(User user, IList<string> roles) =>
        new(user.Id, user.Name, user.Email!, roles.ToList());
}
