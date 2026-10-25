using Audiophile.Application.Products;
using Audiophile.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// Browsing the catalogue is open to everyone and shows only products on sale, the admin side lives in AdminProductsController
[ApiController]
[Route("products")]
public sealed class ProductsController(ProductService _productsService) : ControllerBase
{
  // Lists products with optional category, price range and paging
  [HttpGet]
  public async Task<IActionResult> List(
      ProductCategory? category, decimal? minPrice, decimal? maxPrice, CancellationToken ct,
      int page = 1, int pageSize = ProductQuery.DefaultPageSize) =>
      Ok(await _productsService.ListAsync(
          new ProductQuery(category, minPrice, maxPrice, IncludeHidden: false, page, pageSize), ct));

  // Searches name and description, paged like the list
  [HttpGet("search")]
  public async Task<IActionResult> Search(
      string q, CancellationToken ct, int page = 1, int pageSize = ProductQuery.DefaultPageSize) =>
      Ok(await _productsService.SearchAsync(new ProductSearch(q, IncludeHidden: false, page, pageSize), ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
      Ok(await _productsService.GetAsync(id, ct));
}
