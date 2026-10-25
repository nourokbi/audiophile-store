using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;

namespace Audiophile.Application.Products;

// Optional catalogue filters, IncludeHidden and the filters after PageSize are set only by the admin endpoints, and the page size is capped so one request cannot ask for the whole table
public sealed record ProductQuery(
    ProductCategory? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool IncludeHidden = false,
    int Page = 1,
    int PageSize = ProductQuery.DefaultPageSize,
    string? Search = null,
    ProductAvailability Availability = ProductAvailability.All,
    bool LowStockOnly = false,
    ProductSort Sort = ProductSort.Name)
{
  public const int DefaultPageSize = 12;
  public const int MaxPageSize = 50;
  public const int MaxSearchLength = 100;
  public const int LowStockLimit = 8;
}

// Which products the admin list shows, the shop always sees only the ones on sale
public enum ProductAvailability { All, OnSale, Hidden }

public enum ProductSort { Name, Newest, PriceLowToHigh, PriceHighToLow, StockLowToHigh }

// A search on name and description, paged like the list
public sealed record ProductSearch(
    string Term,
    bool IncludeHidden = false,
    int Page = 1,
    int PageSize = ProductQuery.DefaultPageSize);

// A new product with its first stock count
public sealed record ProductRequest(
    string Name,
    string Description,
    string? Features,
    decimal Price,
    int Stock,
    ProductCategory Category,
    string? ImageUrl,
    Dictionary<string, int>? InTheBox,
    string? Slug = null);       // made from the name when left out, and never changed afterwards

// An edit of a product's details, stock is left out on purpose because it only changes through restock and orders,
// so an edit made with an old stock number cannot undo a checkout, and InTheBox left out keeps the current contents
public sealed record UpdateProductRequest(
    string Name,
    string Description,
    string? Features,
    decimal Price,
    ProductCategory Category,
    string? ImageUrl,
    Dictionary<string, int>? InTheBox);

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
    IReadOnlyDictionary<string, int> InTheBox,
    bool IsActive,
    DateTime CreatedAt)
{
  public static ProductDto From(Product p) => new(
      p.Id, p.Name, p.Slug, p.Description, p.Features, p.Price, p.Stock, p.Stock > 0, p.Category, p.ImageUrl,
      new Dictionary<string, int>(p.InTheBox), p.IsActive, p.CreatedAt);
}

// The bodies of the admin restock, price and availability actions
public sealed record RestockRequest(int Quantity);

public sealed record ChangePriceRequest(decimal Price);

public sealed record SetAvailabilityRequest(bool IsActive);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
  public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
