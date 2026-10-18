namespace Audiophile.Domain.Enums;

public enum PaymentStatus
{
    Pending,      // waiting for the customer to pay
    Succeeded,    // money taken
    Failed        // declined, abandoned or expired
}
