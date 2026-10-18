namespace Audiophile.Domain.Common;

// Shipping costs a flat fee and becomes free once the subtotal reaches the threshold
public static class ShippingPolicy
{
    public const decimal FlatFee = 50m;
    public const decimal FreeShippingThreshold = 5000m;

    public static decimal FeeFor(decimal subTotal) => subTotal >= FreeShippingThreshold ? 0m : FlatFee;
}
