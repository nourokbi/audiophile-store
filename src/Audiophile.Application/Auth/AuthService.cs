using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Auth;

// Register, log in, refresh and log out. ASP.NET Core Identity's UserManager owns
// passwords, lockout and roles (D-01, D-02); this class owns the session tokens.
public sealed class AuthService(
    UserManager<User> users,
    IAppDbContext db,
    ITokenService tokens,
    ILogger<AuthService> logger)
{
    // One message for "no such email" and "wrong password", so the login form
    // cannot be used to find out who has an account.
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidSession = "Your session has expired. Please log in again.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = User.Create(request.Name, request.Email);   // checks name and email; comes with an empty cart

        // Creating the user and giving it a role are two saves; the transaction makes
        // them one, so a failure cannot leave an account without a role.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                throw new ConflictException("An account with this email already exists.");

            // Password rules (length, digits, …), written by Identity for people to read.
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
            // Counts towards lockout: 5 wrong passwords lock the account for 5 minutes.
            await users.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentials);
        }

        await users.ResetAccessFailedCountAsync(user);
        return await StartSessionAsync(user, await users.GetRolesAsync(user), ct);
    }

    // Rotation: every refresh token works once. Trading it in revokes it and issues a new pair.
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == hash, ct)
            ?? throw new UnauthorizedException(InvalidSession);

        if (stored.IsRevoked)
        {
            // Reuse detection: this token was already traded in, so two parties hold a copy
            // and one of them is not the user. End every session this user has.
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

    // Deletes the row rather than revoking it: a revoked token presented again means
    // theft (see RefreshAsync), and logging out on purpose must not look like that.
    public async Task LogoutAsync(Guid userId, RefreshRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hash && t.UserId == userId, ct);
        if (stored is null)
            return;   // already gone: logging out twice is the same outcome

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
