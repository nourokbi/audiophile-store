using Audiophile.Application.Abstractions;
using Audiophile.Application.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// The signed-in customer's saved shipping addresses.
[ApiController]
[Authorize]
[Route("addresses")]
public sealed class AddressesController(AddressService addresses, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await addresses.ListAsync(currentUser.Id, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await addresses.GetAsync(currentUser.Id, id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(AddressRequest request, CancellationToken ct)
    {
        var address = await addresses.CreateAsync(currentUser.Id, request, ct);
        return CreatedAtAction(nameof(Get), new { id = address.Id }, address);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AddressRequest request, CancellationToken ct) =>
        Ok(await addresses.UpdateAsync(currentUser.Id, id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await addresses.DeleteAsync(currentUser.Id, id, ct);
        return NoContent();
    }
}
