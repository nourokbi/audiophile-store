using Audiophile.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Audiophile.Domain.Entities;

// A customer account, Identity owns the password and roles and this class adds the name, addresses, cart and sessions
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
        Email = Guard.Email(email, nameof(email));
        UserName = Email;

        // Every user gets a cart from the start, so no code has to handle a user without one
        Cart = new Cart(Id);
    }

    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Cart Cart { get; private set; } = null!;
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Hand the result to UserManager.CreateAsync and then give it a role with AddToRoleAsync
    public static User Create(string name, string email) => new(name, email);

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

    public RefreshToken IssueRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }
}
