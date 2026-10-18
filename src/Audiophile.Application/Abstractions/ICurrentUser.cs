namespace Audiophile.Application.Abstractions;

// The signed in user of the current request, read from the JWT by the API
public interface ICurrentUser
{
    // Throws when nobody is signed in, so only use it behind Authorize
    Guid Id { get; }
}
