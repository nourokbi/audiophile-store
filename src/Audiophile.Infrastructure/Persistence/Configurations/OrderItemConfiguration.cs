using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Property(i => i.ProductName).HasMaxLength(Product.NameMaxLength);
        b.Property(i => i.ProductImageUrl).HasMaxLength(2048);

        b.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);  // a sold product is deactivated, never deleted

        b.ToTable("OrderItems", t =>
        {
            t.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_OrderItem_UnitPrice_Positive", "\"UnitPrice\" > 0");
        });
    }
}
