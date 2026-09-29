using Audiophile.Domain.Common;

namespace Audiophile.Domain.ValueObjects;

/// <summary>
/// Where an order is going, validated once and then frozen onto the order as
/// columns. It is a copy, not a reference: editing the address it came from
/// must not rewrite where a past order was shipped.
/// </summary>
public sealed record ShippingDetails
{
    public ShippingDetails(
        string fullName,
        string phone,
        string street,
        string city,
        string country,
        string? postalCode)
    {
        FullName = Guard.Text(fullName, 3, 100, nameof(fullName));
        Phone = Guard.Phone(phone, nameof(phone));
        Street = Guard.Text(street, 3, 200, nameof(street));
        City = Guard.Text(city, 2, 100, nameof(city));
        Country = Guard.Text(country, 2, 100, nameof(country));
        PostalCode = Guard.OptionalText(postalCode, 20, nameof(postalCode));
    }

    public string FullName { get; }
    public string Phone { get; }
    public string Street { get; }
    public string City { get; }
    public string Country { get; }
    public string? PostalCode { get; }
}
