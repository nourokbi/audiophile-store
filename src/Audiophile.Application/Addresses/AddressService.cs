using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Addresses;

// A customer's saved addresses, every query is filtered by the customer so someone else's address is simply not found
public sealed class AddressService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AddressDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var addresses = await db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.FullAddressName)
            .ToListAsync(ct);
        return addresses.Select(AddressDto.From).ToList();
    }

    public async Task<AddressDto> GetAsync(Guid userId, Guid addressId, CancellationToken ct) =>
        AddressDto.From(await FindAsync(userId, addressId, ct));

    public async Task<AddressDto> CreateAsync(Guid userId, AddressRequest r, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Your account no longer exists.");

        var address = user.AddAddress(r.FullAddressName, r.Street, r.City, r.Country, r.Phone, r.PostalCode);
        await db.SaveChangesAsync(ct);
        return AddressDto.From(address);
    }

    public async Task<AddressDto> UpdateAsync(Guid userId, Guid addressId, AddressRequest r, CancellationToken ct)
    {
        var address = await FindAsync(userId, addressId, ct);
        address.Update(r.FullAddressName, r.Street, r.City, r.Country, r.Phone, r.PostalCode);
        await db.SaveChangesAsync(ct);
        return AddressDto.From(address);
    }

    // Only deactivates the address so nothing that points at it breaks
    public async Task DeleteAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var address = await FindAsync(userId, addressId, ct);
        address.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    private async Task<Address> FindAsync(Guid userId, Guid addressId, CancellationToken ct) =>
        await db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && a.IsActive, ct)
            ?? throw new NotFoundException($"Address {addressId} was not found.");
}
