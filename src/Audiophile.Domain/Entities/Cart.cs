using Audiophile.Domain.Common;

namespace Audiophile.Domain.Entities;

/// <summary>
/// The cart aggregate. Lines are reached only through this type, which is what
/// keeps the one-line-per-product rule true — the same rule the unique
/// (CartId, ProductId) index enforces in the database.
/// </summary>
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

    /// <summary>Adding a product already in the cart tops up that line rather than creating a second one.</summary>
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

    /// <summary>M4-16 — set a line's quantity outright. To get to zero, call <see cref="RemoveItem"/>.</summary>
    public void SetItemQuantity(Guid productId, int quantity)
    {
        var item = FindItem(productId)
            ?? throw new InvalidOperationException($"Product {productId} is not in this cart.");

        item.SetQuantity(quantity);
    }

    /// <summary>
    /// Throws when the product is not in the cart, so the API answers 404 rather
    /// than a 204 that pretends something was deleted.
    /// </summary>
    public void RemoveItem(Guid productId)
    {
        var item = FindItem(productId)
            ?? throw new InvalidOperationException($"Product {productId} is not in this cart.");

        _cartItems.Remove(item);
    }

    /// <summary>M4-08 and M4-16 — emptied wholesale after checkout, or on request.</summary>
    public void Clear() => _cartItems.Clear();

    public CartItem? FindItem(Guid productId) => _cartItems.SingleOrDefault(i => i.ProductId == productId);
}
