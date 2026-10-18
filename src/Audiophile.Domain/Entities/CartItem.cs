using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

// One product in a cart, created and changed only by Cart
public sealed class CartItem
{
    // An upper limit per product, the real stock check happens at checkout
    public const int MaxQuantity = 100;

    private CartItem() { } // EF Core

    internal CartItem(Guid cartId, Guid productId, int quantity)
    {
        CartId = Guard.NotEmpty(cartId, nameof(cartId));
        ProductId = Guard.NotEmpty(productId, nameof(productId));
        Quantity = Guard.Between(quantity, 1, MaxQuantity, nameof(quantity));
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    public Cart Cart { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    internal void IncreaseQuantity(int amount)
    {
        Guard.Between(amount, 1, MaxQuantity, nameof(amount));

        var total = Quantity + amount;
        if (total > MaxQuantity)
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, $"A cart line cannot exceed {MaxQuantity} units.");

        Quantity = total;
    }

    internal void SetQuantity(int quantity) =>
        Quantity = Guard.Between(quantity, 1, MaxQuantity, nameof(quantity));
}
