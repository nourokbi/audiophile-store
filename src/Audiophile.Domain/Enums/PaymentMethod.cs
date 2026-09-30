namespace Audiophile.Domain.Enums;

public enum PaymentMethod
{
    Card,
    VodafoneCash,     // mobile wallet
    CashOnDelivery,   // no gateway involved
    Kiosk             // cash at an Aman / Masary outlet against a reference number
}
