using Audiophile.Application.Products;
using Audiophile.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// Browsing the catalogue is open to everyone and shows only products on sale, the admin side lives in AdminProductsController
[ApiController]
[Route("products")]
public sealed class ProductsController(ProductService _productsService) : ControllerBase
{
  // The shop page, one page of products with every filter and sort it offers
  [HttpGet]
  public async Task<IActionResult> List(
      ProductCategory? category, decimal? minPrice, decimal? maxPrice, string? q, CancellationToken ct,
      bool inStockOnly = false, bool newOnly = false, ProductSort sort = ProductSort.Name,
      int page = 1, int pageSize = ProductQuery.DefaultPageSize) =>
      Ok(await _productsService.ListAsync(
          new ProductQuery(category, minPrice, maxPrice, IncludeHidden: false, page, pageSize, q,
              Sort: sort, InStockOnly: inStockOnly, NewOnly: newOnly), ct));

  // Searches name and description, paged like the list
  [HttpGet("search")]
  public async Task<IActionResult> Search(
      string q, CancellationToken ct, int page = 1, int pageSize = ProductQuery.DefaultPageSize) =>
      Ok(await _productsService.SearchAsync(new ProductSearch(q, IncludeHidden: false, page, pageSize), ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
      Ok(await _productsService.GetAsync(id, ct));

  [HttpGet("by-slug/{slug}")]
  public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) =>
      Ok(await _productsService.GetBySlugAsync(slug, ct));

  [HttpGet("{id:guid}/related")]
  public async Task<IActionResult> Related(Guid id, CancellationToken ct, int count = ProductQuery.DefaultRelatedCount) =>
      Ok(await _productsService.GetRelatedAsync(id, count, ct));
}
