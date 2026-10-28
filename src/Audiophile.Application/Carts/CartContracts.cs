using Audiophile.Domain.Common;
using Audiophile.Domain.Entities;

namespace Audiophile.Application.Carts;

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record SetCartItemQuantityRequest(int Quantity);

public sealed record CartItemDto(
    Guid ProductId, string ProductName, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);

// One line of the cart a visitor keeps in the browser, sent to be priced or moved into the account after login
public sealed record GuestCartItem(Guid ProductId, int Quantity);

// Every cart endpoint returns the whole cart with today's catalogue prices, so the shop just replaces what it shows
public sealed record CartDto(
    IReadOnlyList<CartItemDto> Items, int TotalQuantity, decimal SubTotal, decimal ShippingFee, decimal Total)
{
    public static readonly CartDto Empty = new([], 0, 0m, 0m, 0m);

    // Needs the products of the items loaded
    public static CartDto From(Cart cart) => From(cart.CartItems.Select(i => (i.Product, i.Quantity)));

    public static CartDto From(IEnumerable<(Product Product, int Quantity)> lines)
    {
        var items = lines
            .Select(l => new CartItemDto(
                l.Product.Id, l.Product.Name, l.Product.ImageUrl, l.Product.Price, l.Quantity, l.Product.Price * l.Quantity))
            .OrderBy(i => i.ProductName)
            .ToList();
        if (items.Count == 0)
            return Empty;

        var subTotal = items.Sum(i => i.LineTotal);
        var shippingFee = ShippingPolicy.FeeFor(subTotal);
        return new CartDto(items, items.Sum(i => i.Quantity), subTotal, shippingFee, subTotal + shippingFee);
    }
}
