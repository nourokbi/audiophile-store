namespace Audiophile.Domain.Enums;

// Stored as text ("Card", "CashOnDelivery"), so adding a method later doesn't renumber anything.
// Wallet and kiosk (Fawry / Aman) were removed on 2026-10-18 to keep the flow small; roadmap
// Phase 2 can add them back as new payment strategies.
public enum PaymentMethod
{
    Card,             // paid on Fawaterak's hosted page
    CashOnDelivery,   // no gateway involved
}
