using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Products;

// The catalogue, browsing is for everyone and editing is for admins
public sealed class ProductService(IAppDbContext db, ILogger<ProductService> logger)
{
  public Task<PagedResult<ProductDto>> ListAsync(ProductQuery q, CancellationToken ct)
  {
    if (q.MinPrice > q.MaxPrice)
      throw new ArgumentException("minPrice cannot be greater than maxPrice.");

    var query = Catalogue(q.IncludeHidden);

    if (q.Category is { } category)
      query = query.Where(p => p.Category == category);
    if (q.MinPrice is { } min)
      query = query.Where(p => p.Price >= min);
    if (q.MaxPrice is { } max)
      query = query.Where(p => p.Price <= max);
    if (!string.IsNullOrWhiteSpace(q.Search))
      query = Matching(query, q.Search);
    if (q.IncludeHidden && q.Availability == ProductAvailability.OnSale)
      query = query.Where(p => p.IsActive);
    if (q.IncludeHidden && q.Availability == ProductAvailability.Hidden)
      query = query.Where(p => !p.IsActive);
    if (q.LowStockOnly)
      query = query.Where(p => p.Stock <= ProductQuery.LowStockLimit);

    return PageAsync(Sorted(query, q.Sort), q.Page, q.PageSize, ct);
  }

  // A case insensitive contains search that works like SQL LIKE '%term%', it reads every row so a pg_trgm or full text index should replace it when the catalogue grows
  public Task<PagedResult<ProductDto>> SearchAsync(ProductSearch s, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(s.Term))
      throw new ArgumentException("q is required.");

    return PageAsync(Sorted(Matching(Catalogue(s.IncludeHidden), s.Term), ProductSort.Name), s.Page, s.PageSize, ct);
  }

  public async Task<ProductDto> GetAsync(Guid id, CancellationToken ct) =>
      ProductDto.From(await FindActiveAsync(id, ct));

  public async Task<ProductDto> CreateAsync(ProductRequest r, CancellationToken ct)
  {
    var product = new Product(r.Name, r.Description, r.Price, r.Stock, r.Category, r.Features, r.ImageUrl, r.Slug);

    // The slug is the product's address in the shop, so two products cannot share one
    if (await db.Products.AnyAsync(p => p.Slug == product.Slug, ct))
      throw new ConflictException($"Another product already uses the address '{product.Slug}'.");

    if (r.InTheBox is not null)
      product.ReplaceBoxContents(r.InTheBox);

    db.Products.Add(product);
    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest r, CancellationToken ct)
  {
    // An admin can edit a hidden product and get it ready before showing it again
    var product = await FindAnyAsync(id, ct);

    product.UpdateDetails(r.Name, r.Description, r.Category, r.Features);
    product.ChangePrice(r.Price);
    product.SetImage(r.ImageUrl);
    if (r.InTheBox is not null)
      product.ReplaceBoxContents(r.InTheBox);

    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  // A checkout can change the same stock at the same moment, so a conflict reloads the product and tries once more
  public Task<ProductDto> RestockAsync(Guid id, int quantity, CancellationToken ct) =>
      db.RetryOnStockConflictAsync(logger, async () =>
      {
        var product = await FindAnyAsync(id, ct);
        product.Restock(quantity);
        await db.SaveChangesAsync(ct);
        return ProductDto.From(product);
      });

  public async Task<ProductDto> ChangePriceAsync(Guid id, decimal price, CancellationToken ct)
  {
    var product = await FindAnyAsync(id, ct);
    product.ChangePrice(price);
    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  // Shows or hides the product, hiding stands in for deleting because past orders still point at it
  public async Task<ProductDto> SetAvailabilityAsync(Guid id, bool isActive, CancellationToken ct)
  {
    var product = await FindAnyAsync(id, ct);
    if (isActive)
      product.Activate();
    else
      product.Deactivate();
    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  // The shop sees only products on sale, an admin can ask for the hidden ones too
  private IQueryable<Product> Catalogue(bool includeHidden)
  {
    var products = db.Products.AsNoTracking();
    return includeHidden ? products : products.Where(p => p.IsActive);
  }

  private static IQueryable<Product> Matching(IQueryable<Product> query, string search)
  {
    if (search.Length > ProductQuery.MaxSearchLength)
      throw new ArgumentException($"q cannot be longer than {ProductQuery.MaxSearchLength} characters.");

    var term = search.Trim().ToLower();
    return query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
  }

  // The name breaks ties, so a page never shows the same product twice when many share a price or a stock
  private static IOrderedQueryable<Product> Sorted(IQueryable<Product> query, ProductSort sort) => sort switch
  {
    ProductSort.Newest => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
    ProductSort.PriceLowToHigh => query.OrderBy(p => p.Price).ThenBy(p => p.Name),
    ProductSort.PriceHighToLow => query.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
    ProductSort.StockLowToHigh => query.OrderBy(p => p.Stock).ThenBy(p => p.Name),
    _ => query.OrderBy(p => p.Name),
  };

  private static async Task<PagedResult<ProductDto>> PageAsync(
      IOrderedQueryable<Product> query, int page, int pageSize, CancellationToken ct)
  {
    if (page < 1)
      throw new ArgumentException("page must be 1 or more.");
    if (pageSize is < 1 or > ProductQuery.MaxPageSize)
      throw new ArgumentException($"pageSize must be between 1 and {ProductQuery.MaxPageSize}.");

    var total = await query.CountAsync(ct);
    var products = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<ProductDto>(products.Select(ProductDto.From).ToList(), page, pageSize, total);
  }

  // The shop only finds products on sale
  private async Task<Product> FindActiveAsync(Guid id, CancellationToken ct) =>
      await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct)
      ?? throw new NotFoundException($"Product {id} was not found.");

  // Admin actions reach hidden products too, so this lookup skips the IsActive check
  private async Task<Product> FindAnyAsync(Guid id, CancellationToken ct) =>
      await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
      ?? throw new NotFoundException($"Product {id} was not found.");
}
