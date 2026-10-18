using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Products;

// The catalogue: browsing for everyone, editing for admins. The rules about a
// product's fields (lengths, positive price, stock never negative) live in Product.
public sealed class ProductService(IAppDbContext db)
{
  public async Task<PagedResult<ProductDto>> ListAsync(ProductQuery q, CancellationToken ct)
  {
    if (q.Page < 1)
      throw new ArgumentException("page must be 1 or more.");
    if (q.PageSize is < 1 or > ProductQuery.MaxPageSize)
      throw new ArgumentException($"pageSize must be between 1 and {ProductQuery.MaxPageSize}.");
    if (q.Search?.Length > ProductQuery.MaxSearchLength)
      throw new ArgumentException($"search cannot be longer than {ProductQuery.MaxSearchLength} characters.");
    if (q.MinPrice > q.MaxPrice)
      throw new ArgumentException("minPrice cannot be greater than maxPrice.");

    var query = db.Products.AsNoTracking().Where(p => p.IsActive);

    if (!string.IsNullOrWhiteSpace(q.Search))
    {
      // ToLower on both sides is case-insensitive on every database. Phase 7 can swap it
      // for PostgreSQL full-text search if the catalogue ever grows large.
      // => this is loaded using LIKE '%term%' which is slow on large tables. We can add a full-text index in the future if needed.
      // or add Elastic Search engine
      var term = q.Search.Trim().ToLower();
      query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
    }

    if (q.Category is { } category)
      query = query.Where(p => p.Category == category);
    if (q.MinPrice is { } min)
      query = query.Where(p => p.Price >= min);
    if (q.MaxPrice is { } max)
      query = query.Where(p => p.Price <= max);

    var total = await query.CountAsync(ct);
    var products = await query
        .OrderBy(p => p.Name)
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .ToListAsync(ct);

    return new PagedResult<ProductDto>(products.Select(ProductDto.From).ToList(), q.Page, q.PageSize, total);
  }

  public async Task<ProductDto> GetAsync(Guid id, CancellationToken ct) =>
      ProductDto.From(await FindActiveAsync(id, ct, tracked: false));

  public async Task<ProductDto> CreateAsync(ProductRequest r, CancellationToken ct)
  {
    var product = new Product(r.Name, r.Description, r.Price, r.Stock, r.Category, r.Features, r.ImageUrl, r.Slug);

    // The slug is the product's URL, so two products can't share one (a unique index backs this up).
    if (await db.Products.AnyAsync(p => p.Slug == product.Slug, ct))
      throw new ConflictException($"Another product already uses the address '{product.Slug}'.");

    if (r.InTheBox is not null)
      product.ReplaceBoxContents(r.InTheBox);

    db.Products.Add(product);
    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  public async Task<ProductDto> UpdateAsync(Guid id, ProductRequest r, CancellationToken ct)
  {
    var product = await FindActiveAsync(id, ct, tracked: true);

    product.UpdateDetails(r.Name, r.Description, r.Category, r.Features);
    product.ChangePrice(r.Price);
    product.CorrectStock(r.Stock);
    product.SetImage(r.ImageUrl);
    product.ReplaceBoxContents(r.InTheBox ?? []);

    await db.SaveChangesAsync(ct);
    return ProductDto.From(product);
  }

  // Deactivates rather than deletes: past orders still point at this product.
  public async Task DeactivateAsync(Guid id, CancellationToken ct)
  {
    var product = await FindActiveAsync(id, ct, tracked: true);
    product.Deactivate();
    await db.SaveChangesAsync(ct);
  }

  private async Task<Product> FindActiveAsync(Guid id, CancellationToken ct, bool tracked)
  {
    var products = tracked ? db.Products : db.Products.AsNoTracking();
    return await products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct)
        ?? throw new NotFoundException($"Product {id} was not found.");
  }
}
