namespace Audiophile.Domain.Enums;

public enum OrderStatus
{
    Pending,       // just placed, payment not confirmed yet
    Confirmed,     // payment confirmed
    Processing,    // being prepared
    Shipped,       // on the way
    Delivered,     // received by customer
    Cancelled
}
