using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        // The id is made in the constructor (Guid.NewGuid()), not by the database. Saying so
        // matters: when a new line is added to a cart that is already saved, EF Core would
        // otherwise see an id that is already set, treat the line as an existing row and
        // try to UPDATE it instead of INSERT it.
        b.Property(i => i.Id).ValueGeneratedNever();

        // One line per product per cart, enforced by the database as well as by Cart.
        b.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();

        b.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);  // a product in someone's cart cannot be deleted

        b.ToTable("CartItems", t => t.HasCheckConstraint(
            "CK_CartItem_Quantity_Range", $"\"Quantity\" BETWEEN 1 AND {CartItem.MaxQuantity}"));
    }
}
