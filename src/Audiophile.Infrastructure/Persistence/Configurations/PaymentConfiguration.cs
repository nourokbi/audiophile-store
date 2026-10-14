using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(p => p.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        b.Property(p => p.Amount).HasPrecision(18, 2);
        b.Property(p => p.RefundedAmount).HasPrecision(18, 2);
        b.Property(p => p.Provider).HasMaxLength(50);
        b.Property(p => p.ProviderReference).HasMaxLength(255);
        b.Property(p => p.ProviderOrderId).HasMaxLength(255);
        b.Property(p => p.ProviderTransactionId).HasMaxLength(255);
        b.Property(p => p.CheckoutUrl).HasMaxLength(2048);
        b.Property(p => p.PaymentCode).HasMaxLength(100);

        // How a webhook without our order id finds its payment.
        b.HasIndex(p => new { p.Provider, p.ProviderOrderId });

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Payment_Amount_Positive", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Payment_RefundedAmount_Valid", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"Amount\"");
        });
    }
}
