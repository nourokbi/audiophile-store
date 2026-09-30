namespace Audiophile.Domain.Enums;

public enum PaymentStatus
{
    Pending,            // created; waiting for the customer or the gateway
    Succeeded,          // money taken
    Failed,             // declined, abandoned or expired
    PartiallyRefunded,  // some of the captured amount sent back
    Refunded,           // all of it sent back
    Voided              // cancelled before settlement, nothing moved
}
