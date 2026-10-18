using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;

namespace Audiophile.API.Auth;

// Reads the signed in user's id from the JWT's sub claim
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new UnauthorizedException("You are not signed in.");
}
