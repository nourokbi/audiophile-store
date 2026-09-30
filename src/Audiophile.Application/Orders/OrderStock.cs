using Audiophile.Domain.Entities;

namespace Audiophile.Application.Orders;

internal static class OrderStock
{
    /// <summary>Cancels the order and puts its units back. Requires items and products loaded.</summary>
    public static void CancelAndRelease(Order order)
    {
        order.Cancel();
        foreach (var item in order.OrderItems)
            item.Product.Restock(item.Quantity);
    }
}

/// <summary>M4-17: the shipping fee is a written rule, not a magic number.</summary>
public static class ShippingPolicy
{
    public const decimal FlatFee = 50m;
    public const decimal FreeShippingThreshold = 5000m;

    public static decimal FeeFor(decimal subTotal) => subTotal >= FreeShippingThreshold ? 0m : FlatFee;
}
