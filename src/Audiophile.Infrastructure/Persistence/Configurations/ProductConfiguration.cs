using System.Text.Json;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Seed rows need fixed values: EF compares them with the model on every
    // migration, so a Guid.NewGuid() or DateTime.UtcNow here would change each time.
    private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Name).HasMaxLength(Product.NameMaxLength);
        b.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        b.Property(p => p.Features).HasMaxLength(Product.FeaturesMaxLength);
        b.Property(p => p.ImageUrl).HasMaxLength(2048);
        b.Property(p => p.Price).HasPrecision(18, 2);
        b.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);

        // "In the box" is a jsonb column, mapped to the private field so the public
        // IReadOnlyDictionary stays read-only. Without the ValueComparer, EF would not
        // notice an item added to the dictionary and would silently not save it.
        b.Ignore(p => p.InTheBox);
        b.Property<Dictionary<string, int>>("_inTheBox")
            .HasColumnName("InTheBox")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => new Dictionary<string, int>(
                    JsonSerializer.Deserialize<Dictionary<string, int>>(v, Json) ?? new Dictionary<string, int>(),
                    StringComparer.OrdinalIgnoreCase),
                new ValueComparer<Dictionary<string, int>>(
                    (a, c) => a!.Count == c!.Count && !a.Except(c).Any(),
                    v => v.OrderBy(kv => kv.Key).Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
                    v => new Dictionary<string, int>(v, StringComparer.OrdinalIgnoreCase)));

        // The domain already refuses these; the constraints stop a hand-written SQL update too.
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Product_Price_Positive", "\"Price\" > 0");
            t.HasCheckConstraint("CK_Product_Stock_NonNegative", "\"Stock\" >= 0");
        });

        // Anonymous objects because Product's setters are private.
        b.HasData(
            Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01", "XX99 Mark II Headphones",
                "The new XX99 Mark II headphones is the pinnacle of pristine audio.",
                14_999m, 25, ProductCategory.Headphones),
            Seed("4ae5911a-421b-4565-b75f-53809c3da182", "XX59 Headphones",   // the id Audiophile.API.http uses
                "Enjoy your audio almost anywhere and customize it to your specific tastes.",
                4_499m, 40, ProductCategory.Headphones),
            Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03", "ZX9 Speaker",
                "Upgrade your sound system with the all new ZX9 active speaker.",
                22_500m, 10, ProductCategory.Speakers),
            Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04", "YX1 Wireless Earphones",
                "Tailor your listening experience with bespoke dynamic drivers.",
                2_999m, 3, ProductCategory.Earphones));
    }

    private static object Seed(
        string id, string name, string description, decimal price, int stock, ProductCategory category) => new
    {
        Id = Guid.Parse(id),
        Name = name,
        Description = description,
        Features = string.Empty,
        Price = price,
        Stock = stock,
        ImageUrl = (string?)null,
        IsActive = true,
        Category = category,
        CreatedAt = SeededAt,
        _inTheBox = new Dictionary<string, int>(),
    };
}
