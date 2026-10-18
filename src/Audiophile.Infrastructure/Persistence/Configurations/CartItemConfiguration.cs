using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        // The id is made in the constructor, so EF must insert a new item instead of updating it
        b.Property(i => i.Id).ValueGeneratedNever();

        // Each product appears once per cart
        b.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();

        b.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.ToTable("CartItems", t => t.HasCheckConstraint(
            "CK_CartItem_Quantity_Range", $"\"Quantity\" BETWEEN 1 AND {CartItem.MaxQuantity}"));
    }
}
