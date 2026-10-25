using Audiophile.Application.Common;
using Audiophile.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// Every catalogue change is here for admins, the logic lives in ProductService and the table reads GET /products?includeHidden=true
[ApiController]
[Route("admin/products")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminProductsController(ProductService _productsService) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
  {
    var product = await _productsService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(ProductsController.Get), "Products", new { id = product.Id }, product);
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct) =>
      Ok(await _productsService.UpdateAsync(id, request, ct));

  // Takes the product off sale while past orders keep pointing at it
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
  {
    await _productsService.SetAvailabilityAsync(id, isActive: false, ct);
    return NoContent();
  }

  [HttpPost("{id:guid}/restock")]
  public async Task<IActionResult> Restock(Guid id, RestockRequest request, CancellationToken ct) =>
      Ok(await _productsService.RestockAsync(id, request.Quantity, ct));

  [HttpPut("{id:guid}/price")]
  public async Task<IActionResult> ChangePrice(Guid id, ChangePriceRequest request, CancellationToken ct) =>
      Ok(await _productsService.ChangePriceAsync(id, request.Price, ct));

  [HttpPut("{id:guid}/availability")]
  public async Task<IActionResult> SetAvailability(Guid id, SetAvailabilityRequest request, CancellationToken ct) =>
      Ok(await _productsService.SetAvailabilityAsync(id, request.IsActive, ct));
}
