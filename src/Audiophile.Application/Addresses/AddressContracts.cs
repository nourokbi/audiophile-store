using Audiophile.Domain.Entities;

namespace Audiophile.Application.Addresses;

// The field rules live in the Address entity and a bad value is answered with 400
public sealed record AddressRequest(
    string FullAddressName, string Street, string City, string Country, string Phone, string? PostalCode);

public sealed record AddressDto(
    Guid Id, string FullAddressName, string Street, string City, string Country, string Phone, string? PostalCode)
{
    public static AddressDto From(Address a) =>
        new(a.Id, a.FullAddressName, a.Street, a.City, a.Country, a.Phone, a.PostalCode);
}
