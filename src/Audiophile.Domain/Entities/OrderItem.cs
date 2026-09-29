using Audiophile.Domain.Common;
using Audiophile.Domain.ValueObjects;

namespace Audiophile.Domain.Entities;

/// <summary>
/// One line of a placed order. Created only by <see cref="Order.Place"/>, from an
/// already-validated <see cref="OrderLine"/>, and immutable afterwards — this row
/// is the record of what was bought, not a working copy.
/// </summary>
public sealed class OrderItem
{
    private OrderItem() { } // EF Core

    internal OrderItem(Guid orderId, OrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        // The line validated its own quantity, price and name on the way in;
        // repeating those checks here would be two places to keep in step.
        OrderId = Guard.NotEmpty(orderId, nameof(orderId));
        ProductId = line.ProductId;
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        ProductName = line.ProductName;
        ProductImageUrl = line.ProductImageUrl;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // These 3 are snapshots — frozen at purchase time, never change
    public decimal UnitPrice { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string ProductImageUrl { get; private set; } = string.Empty;

    public Order Order { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    /// <summary>Derived, not stored — a persisted copy is one more thing that can drift.</summary>
    public decimal LineTotal => UnitPrice * Quantity;
}
