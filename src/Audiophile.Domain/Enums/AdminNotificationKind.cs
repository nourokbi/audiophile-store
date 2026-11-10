namespace Audiophile.Domain.Enums;

// Stored as text, so a new kind needs no schema change
public enum AdminNotificationKind
{
    NewOrder,         // a cash order was placed or a card order was paid
    OrderCancelled,   // a confirmed order was cancelled
    RefundNeeded,     // a paid card order was cancelled and its money must go back
    LowStock          // a sale took a product down to the low stock limit
}
