using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

// One product of an order, with its name and price copied at the moment it was bought
public sealed class OrderItem
{
    private OrderItem() { } // EF Core

    internal OrderItem(Guid orderId, Product product, int quantity)
    {
        OrderId = orderId;
        Product = product;
        ProductId = product.Id;
        Quantity = Guard.Between(quantity, 1, CartItem.MaxQuantity, nameof(quantity));
        UnitPrice = product.Price;
        ProductName = product.Name;
        ProductImageUrl = product.ImageUrl ?? string.Empty;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string ProductImageUrl { get; private set; } = string.Empty;

    public Order Order { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    public decimal LineTotal => UnitPrice * Quantity;
}
