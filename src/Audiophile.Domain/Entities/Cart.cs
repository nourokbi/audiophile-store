using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

// A customer's cart, its items are changed only through this class so each product appears once
public sealed class Cart
{
    private readonly List<CartItem> _cartItems = [];

    private Cart() { } // EF Core

    public Cart(Guid userId) => UserId = Guard.NotEmpty(userId, nameof(userId));

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = null!;
    public IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();

    public bool IsEmpty => _cartItems.Count == 0;
    public int TotalQuantity => _cartItems.Sum(i => i.Quantity);

    // Adding a product already in the cart tops up that line rather than creating a second one.
    public CartItem AddItem(Guid productId, int quantity)
    {
        Guard.NotEmpty(productId, nameof(productId));
        Guard.Between(quantity, 1, CartItem.MaxQuantity, nameof(quantity));

        var existing = FindItem(productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            return existing;
        }

        var item = new CartItem(Id, productId, quantity);
        _cartItems.Add(item);
        return item;
    }

    // Sets the quantity of a product already in the cart, use RemoveItem to take it out
    public void SetItemQuantity(Guid productId, int quantity)
    {
        var item = FindItem(productId)
            ?? throw new InvalidOperationException($"Product {productId} is not in this cart.");

        item.SetQuantity(quantity);
    }

    public void RemoveItem(Guid productId)
    {
        var item = FindItem(productId)
            ?? throw new InvalidOperationException($"Product {productId} is not in this cart.");

        _cartItems.Remove(item);
    }

    public void Clear() => _cartItems.Clear();

    public CartItem? FindItem(Guid productId) => _cartItems.SingleOrDefault(i => i.ProductId == productId);
}
