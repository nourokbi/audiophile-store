using Audiophile.Domain.Common;
using Audiophile.Domain.Entities;

namespace Audiophile.Domain.ValueObjects;

// One validated line handed to Order.Place. Building the list of
// these is where prices get re-read from the catalogue (M4-07); by the time an
// OrderItem exists the numbers are already known to be sound.
public sealed record OrderLine
{
    public const int MaxProductNameLength = 200;

    public OrderLine(Guid productId, int quantity, decimal unitPrice, string productName, string? productImageUrl)
    {
        ProductId = Guard.NotEmpty(productId, nameof(productId));
        Quantity = Guard.Between(quantity, 1, CartItem.MaxQuantity, nameof(quantity));
        UnitPrice = Guard.PositiveMoney(unitPrice, nameof(unitPrice));
        ProductName = Guard.Text(productName, 1, MaxProductNameLength, nameof(productName));
        ProductImageUrl = Guard.OptionalUrl(productImageUrl, nameof(productImageUrl)) ?? string.Empty;
    }

    public Guid ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public string ProductName { get; }
    public string ProductImageUrl { get; }

    public decimal LineTotal => UnitPrice * Quantity;

    // Takes the price and description from the product as it is now, never from the cart.
    public static OrderLine FromProduct(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new OrderLine(product.Id, quantity, product.Price, product.Name, product.ImageUrl);
    }
}
