using Audiophile.Application.Abstractions;
using Audiophile.Application.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// The signed in customer's cart, plus pricing a visitor's browser cart, each endpoint answers with the whole cart
[ApiController]
[Authorize]
[Route("cart")]
public sealed class CartController(CartService carts, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await carts.GetAsync(currentUser.Id, ct));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        Ok(await carts.AddItemAsync(currentUser.Id, request, ct));

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> SetQuantity(Guid productId, SetCartItemQuantityRequest request, CancellationToken ct) =>
        Ok(await carts.SetItemQuantityAsync(currentUser.Id, productId, request.Quantity, ct));

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct) =>
        Ok(await carts.RemoveItemAsync(currentUser.Id, productId, ct));

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct) =>
        Ok(await carts.ClearAsync(currentUser.Id, ct));

    [AllowAnonymous]
    [HttpPost("quote")]
    public async Task<IActionResult> Quote(IReadOnlyList<GuestCartItem> items, CancellationToken ct) =>
        Ok(await carts.QuoteAsync(items, ct));

    [HttpPost("merge")]
    public async Task<IActionResult> Merge(IReadOnlyList<GuestCartItem> items, CancellationToken ct) =>
        Ok(await carts.MergeAsync(currentUser.Id, items, ct));
}
