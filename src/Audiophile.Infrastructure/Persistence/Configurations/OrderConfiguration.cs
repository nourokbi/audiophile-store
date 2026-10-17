using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        b.Property(o => o.SubTotal).HasPrecision(18, 2);
        b.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        b.Property(o => o.ShippingFee).HasPrecision(18, 2);
        b.Property(o => o.TotalAmount).HasPrecision(18, 2);
        b.Property(o => o.IdempotencyKey).HasMaxLength(Order.IdempotencyKeyMaxLength);

        // Shipping snapshot: the same limits as ShippingDetails.
        b.Property(o => o.ShippingFullName).HasMaxLength(100);
        b.Property(o => o.ShippingPhone).HasMaxLength(20);
        b.Property(o => o.ShippingStreet).HasMaxLength(200);
        b.Property(o => o.ShippingCity).HasMaxLength(100);
        b.Property(o => o.ShippingCountry).HasMaxLength(100);
        b.Property(o => o.ShippingPostalCode).HasMaxLength(20);

        // M4-04: the same key from the same customer can only ever create one order.
        b.HasIndex(o => new { o.UserId, o.IdempotencyKey }).IsUnique();
        b.HasIndex(o => o.Status);

        b.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);  // orders are financial records: deleting a user must not erase them

        b.HasMany(o => o.OrderItems)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Order_SubTotal_NonNegative", "\"SubTotal\" >= 0");
            t.HasCheckConstraint("CK_Order_DiscountAmount_Valid", "\"DiscountAmount\" >= 0 AND \"DiscountAmount\" <= \"SubTotal\"");
            t.HasCheckConstraint("CK_Order_ShippingFee_NonNegative", "\"ShippingFee\" >= 0");
            t.HasCheckConstraint("CK_Order_TotalAmount_NonNegative", "\"TotalAmount\" >= 0");
        });
    }
}
