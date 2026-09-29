using Audiophile.Domain.Common;
using Audiophile.Domain.ValueObjects;

namespace Audiophile.Domain.Entities;

public sealed class Address
{
    private Address() { } // EF Core

    public Address(
        Guid userId,
        string fullAddressName,
        string street,
        string city,
        string country,
        string phone,
        string? postalCode = null)
    {
        UserId = Guard.NotEmpty(userId, nameof(userId));
        FullAddressName = Guard.Text(fullAddressName, 3, 100, nameof(fullAddressName));
        Street = Guard.Text(street, 3, 200, nameof(street));
        City = Guard.Text(city, 2, 100, nameof(city));
        Country = Guard.Text(country, 2, 100, nameof(country));
        Phone = Guard.Phone(phone, nameof(phone));
        PostalCode = Guard.OptionalText(postalCode, 20, nameof(postalCode));
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string FullAddressName { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    public User User { get; private set; } = null!;

    public void Update(
        string fullAddressName,
        string street,
        string city,
        string country,
        string phone,
        string? postalCode)
    {
        if (!IsActive)
            throw new InvalidOperationException("A deactivated address cannot be edited.");

        // Everything is validated before anything is assigned, so a rejected edit
        // leaves the address exactly as it was rather than half-updated.
        var name = Guard.Text(fullAddressName, 3, 100, nameof(fullAddressName));
        var newStreet = Guard.Text(street, 3, 200, nameof(street));
        var newCity = Guard.Text(city, 2, 100, nameof(city));
        var newCountry = Guard.Text(country, 2, 100, nameof(country));
        var newPhone = Guard.Phone(phone, nameof(phone));
        var newPostalCode = Guard.OptionalText(postalCode, 20, nameof(postalCode));

        FullAddressName = name;
        Street = newStreet;
        City = newCity;
        Country = newCountry;
        Phone = newPhone;
        PostalCode = newPostalCode;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    /// Copies the address into the frozen form an order carries.
    public ShippingDetails ToShippingDetails() =>
        new(FullAddressName, Phone, Street, City, Country, PostalCode);
}
