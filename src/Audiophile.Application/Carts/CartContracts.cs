using Audiophile.Domain.Common;
using Audiophile.Domain.Entities;

namespace Audiophile.Application.Carts;

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record SetCartItemQuantityRequest(int Quantity);

public sealed record CartItemDto(
    Guid ProductId, string ProductName, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);

// Every cart endpoint returns the whole cart with today's catalogue prices, so the shop just replaces what it shows
public sealed record CartDto(
    IReadOnlyList<CartItemDto> Items, int TotalQuantity, decimal SubTotal, decimal ShippingFee, decimal Total)
{
    public static readonly CartDto Empty = new([], 0, 0m, 0m, 0m);

    // Needs the products of the items loaded
    public static CartDto From(Cart cart)
    {
        if (cart.IsEmpty)
            return Empty;

        var items = cart.CartItems
            .Select(i => new CartItemDto(
                i.ProductId, i.Product.Name, i.Product.ImageUrl, i.Product.Price, i.Quantity, i.Product.Price * i.Quantity))
            .OrderBy(i => i.ProductName)
            .ToList();

        var subTotal = items.Sum(i => i.LineTotal);
        var shippingFee = ShippingPolicy.FeeFor(subTotal);
        return new CartDto(items, cart.TotalQuantity, subTotal, shippingFee, subTotal + shippingFee);
    }
}
