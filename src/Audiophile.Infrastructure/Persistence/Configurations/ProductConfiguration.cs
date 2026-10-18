using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

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
    b.Property(p => p.Slug).HasMaxLength(Product.SlugMaxLength);
    b.HasIndex(p => p.Slug).IsUnique();
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

    // Optimistic concurrency: PostgreSQL's xmin system column, as a shadow property.
    b.Property<uint>("Version").IsRowVersion();

    // The domain already refuses these; the constraints stop a hand-written SQL update too.
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Product_Price_Positive", "\"Price\" > 0");
      t.HasCheckConstraint("CK_Product_Stock_NonNegative", "\"Stock\" >= 0");
    });

    // The six products the React storefront shows (client/src/data/data.json): same names,
    // slugs, text and box contents, so the shop's pages and the API describe the same things.
    // Prices are in EGP. Anonymous objects because Product's setters are private.
    b.HasData(
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01", "xx99-mark-two-headphones", "XX99 Mark II Headphones",
            ProductCategory.Headphones, price: 2999m, stock: 25,
            imageUrl: "/assets/cart/image-xx99-mark-two-headphones.jpg",
            description: "The new XX99 Mark II headphones is the pinnacle of pristine audio. It redefines your premium headphone experience by reproducing the balanced depth and precision of studio-quality sound.",
            features: "Featuring a genuine leather head strap and premium earcups, these headphones deliver superior comfort for those who like to enjoy endless listening. It includes intuitive controls designed for any situation. Whether you’re taking a business call or just in your own personal space, the auto on/off and pause features ensure that you’ll never miss a beat.\n\nThe advanced Active Noise Cancellation with built-in equalizer allow you to experience your audio world on your terms. It lets you enjoy your audio in peace, but quickly interact with your surroundings when you need to. Combined with Bluetooth 5. 0 compliant connectivity and 17 hour battery life, the XX99 Mark II headphones gives you superior sound, cutting-edge technology, and a modern design aesthetic.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1, ["Travel bag"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05", "xx99-mark-one-headphones", "XX99 Mark I Headphones",
            ProductCategory.Headphones, price: 1750m, stock: 20,
            imageUrl: "/assets/cart/image-xx99-mark-one-headphones.jpg",
            description: "As the gold standard for headphones, the classic XX99 Mark I offers detailed and accurate audio reproduction for audiophiles, mixing engineers, and music aficionados alike in studios and on the go.",
            features: "As the headphones all others are measured against, the XX99 Mark I demonstrates over five decades of audio expertise, redefining the critical listening experience. This pair of closed-back headphones are made of industrial, aerospace-grade materials to emphasize durability at a relatively light weight of 11 oz.\n\nFrom the handcrafted microfiber ear cushions to the robust metal headband with inner damping element, the components work together to deliver comfort and uncompromising sound. Its closed-back design delivers up to 27 dB of passive noise cancellation, reducing resonance by reflecting sound to a dedicated absorber. For connectivity, a specially tuned cable is includes with a balanced gold connector.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1 }),
        Seed("4ae5911a-421b-4565-b75f-53809c3da182", "xx59-headphones", "XX59 Headphones",
            ProductCategory.Headphones, price: 899m, stock: 40,
            imageUrl: "/assets/cart/image-xx59-headphones.jpg",
            description: "Enjoy your audio almost anywhere and customize it to your specific tastes with the XX59 headphones. The stylish yet durable versatile wireless headset is a brilliant companion at home or on the move.",
            features: "These headphones have been created from durable, high-quality materials tough enough to take anywhere. Its compact folding design fuses comfort and minimalist style making it perfect for travel. Flawless transmission is assured by the latest wireless technology engineered for audio synchronization with videos.\n\nMore than a simple pair of headphones, this headset features a pair of built-in microphones for clear, hands-free calling when paired with a compatible smartphone. Controlling music and calls is also intuitive thanks to easy-access touch buttons on the earcups. Regardless of how you use the XX59 headphones, you can do so all day thanks to an impressive 30-hour battery life that can be rapidly recharged via USB-C.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03", "zx9-speaker", "ZX9 Speaker",
            ProductCategory.Speakers, price: 4500m, stock: 10,
            imageUrl: "/assets/cart/image-zx9-speaker.jpg",
            description: "Upgrade your sound system with the all new ZX9 active speaker. It’s a bookshelf speaker system that offers truly wireless connectivity -- creating new possibilities for more pleasing and practical audio setups.",
            features: "Connect via Bluetooth or nearly any wired source. This speaker features optical, digital coaxial, USB Type-B, stereo RCA, and stereo XLR inputs, allowing you to have up to five wired source devices connected for easy switching. Improved bluetooth technology offers near lossless audio quality at up to 328ft (100m).\n\nDiscover clear, more natural sounding highs than the competition with ZX9’s signature planar diaphragm tweeter. Equally important is its powerful room-shaking bass courtesy of a 6.5” aluminum alloy bass unit. You’ll be able to enjoy equal sound quality whether in a large room or small den. Furthermore, you will experience new sensations from old songs since it can respond to even the subtle waveforms.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Speaker cloth panel"] = 2, ["User manual"] = 1, ["3.5mm 10m audio cable"] = 1, ["10m optical cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06", "zx7-speaker", "ZX7 Speaker",
            ProductCategory.Speakers, price: 3500m, stock: 15,
            imageUrl: "/assets/cart/image-zx7-speaker.jpg",
            description: "Stream high quality sound wirelessly with minimal to no loss. The ZX7 speaker uses high-end audiophile components that represents the top of the line powered speakers for home or studio use.",
            features: "Reap the advantages of a flat diaphragm tweeter cone. This provides a fast response rate and excellent high frequencies that lower tiered bookshelf speakers cannot provide. The woofers are made from aluminum that produces a unique and clear sound. XLR inputs allow you to connect to a mixer for more advanced usage.\n\nThe ZX7 speaker is the perfect blend of stylish design and high performance. It houses an encased MDF wooden enclosure which minimises acoustic resonance. Dual connectivity allows pairing through bluetooth or traditional optical and RCA input. Switch input sources and control volume at your finger tips with the included wireless remote. This versatile speaker is equipped to deliver an authentic listening experience.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Speaker cloth panel"] = 2, ["User manual"] = 1, ["3.5mm 7.5m audio cable"] = 1, ["7.5m optical cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04", "yx1-earphones", "YX1 Wireless Earphones",
            ProductCategory.Earphones, price: 599m, stock: 30,
            imageUrl: "/assets/cart/image-yx1-earphones.jpg",
            description: "Tailor your listening experience with bespoke dynamic drivers from the new YX1 Wireless Earphones. Enjoy incredible high-fidelity sound even in noisy environments with its active noise cancellation feature.",
            features: "Experience unrivalled stereo sound thanks to innovative acoustic technology. With improved ergonomics designed for full day wearing, these revolutionary earphones have been finely crafted to provide you with the perfect fit, delivering complete comfort all day long while enjoying exceptional noise isolation and truly immersive sound.\n\nThe YX1 Wireless Earphones features customizable controls for volume, music, calls, and voice assistants built into both earbuds. The new 7-hour battery life can be extended up to 28 hours with the charging case, giving you uninterrupted play time. Exquisite craftsmanship with a splash resistant design now available in an all new white and grey color scheme as well as the popular classic black.",
            inTheBox: new() { ["Earphone unit"] = 2, ["Multi-size earplugs"] = 6, ["User manual"] = 1, ["USB-C charging cable"] = 1, ["Travel pouch"] = 1 }));
  }

  private static object Seed(
      string id, string slug, string name, ProductCategory category, decimal price, int stock,
      string imageUrl, string description, string features, Dictionary<string, int> inTheBox) => new
      {
        Id = Guid.Parse(id),
        Name = name,
        Slug = slug,
        Description = description,
        Features = features,
        Price = price,
        Stock = stock,
        ImageUrl = (string?)imageUrl,
        IsActive = true,
        Category = category,
        CreatedAt = SeededAt,
        _inTheBox = inTheBox,
      };
}
