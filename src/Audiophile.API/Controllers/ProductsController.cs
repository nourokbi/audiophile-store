using Audiophile.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.API.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController(IAppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Price, p.Stock, p.Category })
            .ToListAsync(ct);

        return Ok(products.OrderBy(p => p.Name));
    }
}
