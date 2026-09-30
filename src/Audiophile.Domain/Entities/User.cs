using Audiophile.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Audiophile.Domain.Entities;

// The account aggregate. Identity owns the credentials — email, password hash,
// phone, lockout, stamps — and, since D-02, the roles as well. What is added
// here is the shop's own view of a customer: their display name, their
// addresses, and the sessions they hold.
// Keyed on Guid rather than Identity's default string, so
// every UserId foreign key in the model is the same type as this key.
public class User : IdentityUser<Guid>
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 50;

    private readonly List<Address> _addresses = [];
    private readonly List<Order> _orders = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { } // EF Core

    private User(string name, string email)
    {
        Id = Guid.NewGuid();
        Name = Guard.Text(name, NameMinLength, NameMaxLength, nameof(name));

        var address = Guard.Email(email, nameof(email));
        Email = address;
        UserName = address;

        // Every user has exactly one cart from the moment they exist, so no code
        // downstream has to answer "what if this user has no cart yet".
        Cart = new Cart(Id);
    }

    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Exposed read-only: the lists are mutated through the methods below, which
    // are the only place the rules about them live.
    public Cart Cart { get; private set; } = null!;
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // The only way a user comes into existence. Role assignment is a separate
    // step now that Identity owns roles: hand the result to
    // UserManager.CreateAsync, then AddToRoleAsync.
    public static User Create(string name, string email) => new(name, email);

    public void UpdateName(string newName) =>
        Name = Guard.Text(newName, NameMinLength, NameMaxLength, nameof(newName));

    public void UpdateEmail(string newEmail)
    {
        var address = Guard.Email(newEmail, nameof(newEmail));

        Email = address;
        UserName = address;
        EmailConfirmed = false;
    }

    public void SetPhone(string? phone) =>
        PhoneNumber = phone is null ? null : Guard.Phone(phone, nameof(phone));

    // Requires Addresses to have been loaded.
    public Address AddAddress(
        string fullAddressName,
        string street,
        string city,
        string country,
        string phone,
        string? postalCode = null)
    {
        var address = new Address(Id, fullAddressName, street, city, country, phone, postalCode);
        _addresses.Add(address);
        return address;
    }

    // Soft-deletes: orders snapshot their shipping address, but a row that is
    // still referenced anywhere is safer deactivated than deleted.
    public void RemoveAddress(Guid addressId)
    {
        var address = FindAddress(addressId)
            ?? throw new InvalidOperationException($"Address {addressId} does not belong to this user.");

        address.Deactivate();
    }

    public Address? FindAddress(Guid addressId) =>
        _addresses.SingleOrDefault(a => a.Id == addressId && a.IsActive);

    // Requires RefreshTokens to have been loaded.
    public RefreshToken IssueRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    // Logout, and the blunt instrument for "this account may be compromised".
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens)
        {
            if (token.IsActive)
                token.Revoke();
        }
    }
}
