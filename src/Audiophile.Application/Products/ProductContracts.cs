using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;

namespace Audiophile.Application.Products;

// Catalogue filters. Every one is optional; page size is capped so one request
// can never ask for the whole table.
public sealed record ProductQuery(
    string? Search = null,
    ProductCategory? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = ProductQuery.DefaultPageSize)
{
  public const int DefaultPageSize = 12;
  public const int MaxPageSize = 50;
  public const int MaxSearchLength = 100;
}

// Create and update share one shape: an update replaces every field.
public sealed record ProductRequest(
    string Name,
    string Description,
    string? Features,
    decimal Price,
    int Stock,
    ProductCategory Category,
    string? ImageUrl,
    Dictionary<string, int>? InTheBox,
    string? Slug = null);       // create only; made from the name when left out. An update never changes it.

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Features,
    decimal Price,
    int Stock,
    bool InStock,
    ProductCategory Category,
    string? ImageUrl,
    IReadOnlyDictionary<string, int> InTheBox)
{
  public static ProductDto From(Product p) => new(
      p.Id, p.Name, p.Slug, p.Description, p.Features, p.Price, p.Stock, p.Stock > 0, p.Category, p.ImageUrl,
      new Dictionary<string, int>(p.InTheBox));
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
  public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
