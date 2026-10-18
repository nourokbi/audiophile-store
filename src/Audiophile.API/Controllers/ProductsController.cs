using Audiophile.Application.Common;
using Audiophile.Application.Products;
using Audiophile.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// Browsing is open to everyone; changing the catalogue is for admins.
[ApiController]
[Route("products")]
public sealed class ProductsController(ProductService _productsService) : ControllerBase
{
  // GET /products?search=xx&category=Headphones&minPrice=100&maxPrice=5000&page=1&pageSize=12
  [HttpGet]
  public async Task<IActionResult> List(
      string? search, ProductCategory? category, decimal? minPrice, decimal? maxPrice,
      CancellationToken ct, int page = 1, int pageSize = ProductQuery.DefaultPageSize) =>
      Ok(await _productsService.ListAsync(new ProductQuery(search, category, minPrice, maxPrice, page, pageSize), ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
      Ok(await _productsService.GetAsync(id, ct));

  [Authorize(Roles = Roles.Admin)]
  [HttpPost]
  public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
  {
    var product = await _productsService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
  }

  [Authorize(Roles = Roles.Admin)]
  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct) =>
      Ok(await _productsService.UpdateAsync(id, request, ct));

  // Takes the product off sale; past orders keep pointing at it.
  [Authorize(Roles = Roles.Admin)]
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
  {
    await _productsService.DeactivateAsync(id, ct);
    return NoContent();
  }
}
