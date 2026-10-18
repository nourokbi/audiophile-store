namespace Audiophile.Domain.Enums;

// Stored as text, so adding a method later does not renumber anything
public enum PaymentMethod
{
    Card,             // paid on Fawaterak's hosted page
    CashOnDelivery,   // no gateway involved
}
