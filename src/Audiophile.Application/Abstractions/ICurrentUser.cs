namespace Audiophile.Application.Abstractions;

// The signed-in user of the current request. The API implements it from the JWT,
// so use cases never touch HttpContext.
public interface ICurrentUser
{
    // Throws when nobody is signed in; only call it behind [Authorize].
    Guid Id { get; }
}
