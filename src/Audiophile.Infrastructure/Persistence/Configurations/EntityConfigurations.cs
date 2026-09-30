using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Name).HasMaxLength(Product.NameMaxLength);
        b.Property(p => p.Price).HasPrecision(18, 2);
        b.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);

        // The jsonb "in the box" mapping is PostgreSQL work from the build plan (B3);
        // the payment slice runs on SQLite and does not need it.
        b.Ignore(p => p.InTheBox);
    }
}

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

        // M4-04: the same key from the same customer can only ever create one order.
        b.HasIndex(o => new { o.UserId, o.IdempotencyKey }).IsUnique();

        b.HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId);
        b.HasMany(o => o.OrderItems).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        b.HasOne(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Payment>(p => p.OrderId);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

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
    }
}

internal sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> b)
    {
        b.Property(e => e.Provider).HasMaxLength(50);
        b.Property(e => e.ExternalId).HasMaxLength(255);
        b.Property(e => e.EventType).HasMaxLength(100);

        // M4-11 / M5-05: the database, not an if-statement, guarantees each gateway
        // event is applied at most once, even when two deliveries arrive together.
        b.HasIndex(e => new { e.Provider, e.ExternalId }).IsUnique();
        b.HasIndex(e => e.PaymentId);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Name).HasMaxLength(User.NameMaxLength);
        b.HasOne(u => u.Cart).WithOne(c => c.User).HasForeignKey<Cart>(c => c.UserId);
    }
}

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.HasMany(c => c.CartItems).WithOne(i => i.Cart).HasForeignKey(i => i.CartId);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        b.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
    }
}
