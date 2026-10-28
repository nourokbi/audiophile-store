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

  // Seed rows need fixed values because EF compares them with the model on every migration
  private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

  public void Configure(EntityTypeBuilder<Product> b)
  {
    b.Property(p => p.Name).HasMaxLength(Product.NameMaxLength);
    b.Property(p => p.Slug).HasMaxLength(Product.SlugMaxLength);
    b.HasIndex(p => p.Slug).IsUnique();
    b.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
    b.Property(p => p.Features).HasMaxLength(Product.FeaturesMaxLength);
    b.Ignore(p => p.ImageUrl);
    b.Property(p => p.Price).HasPrecision(18, 2);
    b.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);

    // A jsonb column mapped to the private field, the value comparer lets EF notice changes inside the dictionary
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

    // The photos as a jsonb list mapped to the private field, compared item by item so a reorder is saved
    b.Ignore(p => p.Images);
    b.Property<List<string>>("_images")
        .HasColumnName("Images")
        .HasColumnType("jsonb")
        .HasConversion(
            v => JsonSerializer.Serialize(v, Json),
            v => JsonSerializer.Deserialize<List<string>>(v, Json) ?? new List<string>(),
            new ValueComparer<List<string>>(
                (a, c) => a!.SequenceEqual(c!),
                v => v.Aggregate(0, (hash, image) => HashCode.Combine(hash, image)),
                v => v.ToList()));

    // PostgreSQL's xmin column detects two checkouts changing the same stock at once
    b.Property<uint>("Version").IsRowVersion();

    // The entity already refuses these, the constraints also stop a hand written SQL update
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Product_Price_Positive", "\"Price\" > 0");
      t.HasCheckConstraint("CK_Product_Stock_NonNegative", "\"Stock\" >= 0");
    });

    // The shop's products with prices in EGP, as anonymous objects because the setters are private
    b.HasData(
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01", "xx99-mark-two-headphones", "XX99 Mark II Headphones",
            ProductCategory.Headphones, price: 2999m, stock: 25,
            imageUrl: "/assets/cart/image-xx99-mark-two-headphones.jpg", gallery: Gallery("xx99-mark-two-headphones"),
            description: "The new XX99 Mark II headphones is the pinnacle of pristine audio. It redefines your premium headphone experience by reproducing the balanced depth and precision of studio-quality sound.",
            features: "Featuring a genuine leather head strap and premium earcups, these headphones deliver superior comfort for those who like to enjoy endless listening. It includes intuitive controls designed for any situation. Whether you’re taking a business call or just in your own personal space, the auto on/off and pause features ensure that you’ll never miss a beat.\n\nThe advanced Active Noise Cancellation with built-in equalizer allow you to experience your audio world on your terms. It lets you enjoy your audio in peace, but quickly interact with your surroundings when you need to. Combined with Bluetooth 5. 0 compliant connectivity and 17 hour battery life, the XX99 Mark II headphones gives you superior sound, cutting-edge technology, and a modern design aesthetic.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1, ["Travel bag"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05", "xx99-mark-one-headphones", "XX99 Mark I Headphones",
            ProductCategory.Headphones, price: 1750m, stock: 20,
            imageUrl: "/assets/cart/image-xx99-mark-one-headphones.jpg", gallery: Gallery("xx99-mark-one-headphones"),
            description: "As the gold standard for headphones, the classic XX99 Mark I offers detailed and accurate audio reproduction for audiophiles, mixing engineers, and music aficionados alike in studios and on the go.",
            features: "As the headphones all others are measured against, the XX99 Mark I demonstrates over five decades of audio expertise, redefining the critical listening experience. This pair of closed-back headphones are made of industrial, aerospace-grade materials to emphasize durability at a relatively light weight of 11 oz.\n\nFrom the handcrafted microfiber ear cushions to the robust metal headband with inner damping element, the components work together to deliver comfort and uncompromising sound. Its closed-back design delivers up to 27 dB of passive noise cancellation, reducing resonance by reflecting sound to a dedicated absorber. For connectivity, a specially tuned cable is includes with a balanced gold connector.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1 }),
        Seed("4ae5911a-421b-4565-b75f-53809c3da182", "xx59-headphones", "XX59 Headphones",
            ProductCategory.Headphones, price: 899m, stock: 40,
            imageUrl: "/assets/cart/image-xx59-headphones.jpg", gallery: Gallery("xx59-headphones"),
            description: "Enjoy your audio almost anywhere and customize it to your specific tastes with the XX59 headphones. The stylish yet durable versatile wireless headset is a brilliant companion at home or on the move.",
            features: "These headphones have been created from durable, high-quality materials tough enough to take anywhere. Its compact folding design fuses comfort and minimalist style making it perfect for travel. Flawless transmission is assured by the latest wireless technology engineered for audio synchronization with videos.\n\nMore than a simple pair of headphones, this headset features a pair of built-in microphones for clear, hands-free calling when paired with a compatible smartphone. Controlling music and calls is also intuitive thanks to easy-access touch buttons on the earcups. Regardless of how you use the XX59 headphones, you can do so all day thanks to an impressive 30-hour battery life that can be rapidly recharged via USB-C.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Replacement earcups"] = 2, ["User manual"] = 1, ["3.5mm 5m audio cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03", "zx9-speaker", "ZX9 Speaker",
            ProductCategory.Speakers, price: 4500m, stock: 10,
            imageUrl: "/assets/cart/image-zx9-speaker.jpg", gallery: Gallery("zx9-speaker"),
            description: "Upgrade your sound system with the all new ZX9 active speaker. It’s a bookshelf speaker system that offers truly wireless connectivity -- creating new possibilities for more pleasing and practical audio setups.",
            features: "Connect via Bluetooth or nearly any wired source. This speaker features optical, digital coaxial, USB Type-B, stereo RCA, and stereo XLR inputs, allowing you to have up to five wired source devices connected for easy switching. Improved bluetooth technology offers near lossless audio quality at up to 328ft (100m).\n\nDiscover clear, more natural sounding highs than the competition with ZX9’s signature planar diaphragm tweeter. Equally important is its powerful room-shaking bass courtesy of a 6.5” aluminum alloy bass unit. You’ll be able to enjoy equal sound quality whether in a large room or small den. Furthermore, you will experience new sensations from old songs since it can respond to even the subtle waveforms.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Speaker cloth panel"] = 2, ["User manual"] = 1, ["3.5mm 10m audio cable"] = 1, ["10m optical cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06", "zx7-speaker", "ZX7 Speaker",
            ProductCategory.Speakers, price: 3500m, stock: 15,
            imageUrl: "/assets/cart/image-zx7-speaker.jpg", gallery: Gallery("zx7-speaker"),
            description: "Stream high quality sound wirelessly with minimal to no loss. The ZX7 speaker uses high-end audiophile components that represents the top of the line powered speakers for home or studio use.",
            features: "Reap the advantages of a flat diaphragm tweeter cone. This provides a fast response rate and excellent high frequencies that lower tiered bookshelf speakers cannot provide. The woofers are made from aluminum that produces a unique and clear sound. XLR inputs allow you to connect to a mixer for more advanced usage.\n\nThe ZX7 speaker is the perfect blend of stylish design and high performance. It houses an encased MDF wooden enclosure which minimises acoustic resonance. Dual connectivity allows pairing through bluetooth or traditional optical and RCA input. Switch input sources and control volume at your finger tips with the included wireless remote. This versatile speaker is equipped to deliver an authentic listening experience.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Speaker cloth panel"] = 2, ["User manual"] = 1, ["3.5mm 7.5m audio cable"] = 1, ["7.5m optical cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04", "yx1-earphones", "YX1 Wireless Earphones",
            ProductCategory.Earphones, price: 599m, stock: 30,
            imageUrl: "/assets/cart/image-yx1-earphones.jpg", gallery: Gallery("yx1-earphones"),
            description: "Tailor your listening experience with bespoke dynamic drivers from the new YX1 Wireless Earphones. Enjoy incredible high-fidelity sound even in noisy environments with its active noise cancellation feature.",
            features: "Experience unrivalled stereo sound thanks to innovative acoustic technology. With improved ergonomics designed for full day wearing, these revolutionary earphones have been finely crafted to provide you with the perfect fit, delivering complete comfort all day long while enjoying exceptional noise isolation and truly immersive sound.\n\nThe YX1 Wireless Earphones features customizable controls for volume, music, calls, and voice assistants built into both earbuds. The new 7-hour battery life can be extended up to 28 hours with the charging case, giving you uninterrupted play time. Exquisite craftsmanship with a splash resistant design now available in an all new white and grey color scheme as well as the popular classic black.",
            inTheBox: new() { ["Earphone unit"] = 2, ["Multi-size earplugs"] = 6, ["User manual"] = 1, ["USB-C charging cable"] = 1, ["Travel pouch"] = 1 }),

        // The new collection, with photos from the Stitch project
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01", "studio-reference-headphones", "Studio Reference Headphones",
            ProductCategory.Headphones, price: 3299m, stock: 18,
            imageUrl: "/assets/products/studio-reference-headphones.jpg",
            description: "Closed-back studio headphones with a lambskin leather headband and machined brass hinges, tuned flat for mixing and long listening sessions.",
            features: "50mm dynamic drivers with a neutral tuning that tells you what is really in the recording. Memory foam earcups seal out the room without pressing on your ears.\n\nThe brass hinges fold flat for travel, and the detachable cable locks into place so it never slips out mid session.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Detachable 3m cable"] = 1, ["6.35mm adapter"] = 1, ["Carry case"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02", "planar-pro-headphones", "Planar Pro Headphones",
            ProductCategory.Headphones, price: 5299m, stock: 8,
            imageUrl: "/assets/products/planar-pro-headphones.jpg",
            description: "Open-back planar magnetic headphones with brushed copper details, made for listeners who want every texture of the music.",
            features: "Planar magnetic drivers move the whole diaphragm at once, giving fast and clean detail with very low distortion. The open back creates a wide and natural soundstage.\n\nA light aluminum frame and suspended headband spread the weight, so they stay comfortable for hours.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Balanced 4.4mm cable"] = 1, ["3.5mm cable"] = 1, ["Hard case"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03", "bronze-edition-headphones", "Bronze Edition Headphones",
            ProductCategory.Headphones, price: 2799m, stock: 14,
            imageUrl: "/assets/products/bronze-edition-headphones.jpg",
            description: "Matte black over-ear headphones with polished bronze arms and plush leather earcups, a limited edition of our classic sound.",
            features: "Warm and full bodied tuning with a deep, controlled bass. The bronze arms are machined from solid metal and finished by hand.\n\nWireless with 30 hours of battery, or wired when you want the purest signal.",
            inTheBox: new() { ["Headphone unit"] = 1, ["USB-C charging cable"] = 1, ["3.5mm cable"] = 1, ["Travel pouch"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04", "closed-back-monitor-headphones", "Closed-Back Monitor Headphones",
            ProductCategory.Headphones, price: 2199m, stock: 25,
            imageUrl: "/assets/products/closed-back-monitor-headphones.jpg",
            description: "Everyday monitor headphones with lambskin earcups and a braided copper cable, ready for the studio desk or the commute.",
            features: "Closed-back design keeps your music in and the noise out. A balanced tuning works for recording, editing and simply enjoying music.\n\nThe braided cable resists tangles and the earcups can be replaced when they wear.",
            inTheBox: new() { ["Headphone unit"] = 1, ["Braided cable"] = 1, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05", "zx12-tower-speaker", "ZX12 Tower Speaker",
            ProductCategory.Speakers, price: 8499m, stock: 4,
            imageUrl: "/assets/products/zx12-tower-speaker.jpg",
            description: "A tall floorstanding speaker with a beryllium dome tweeter and carbon weave woofers, built to fill a large room.",
            features: "The beryllium tweeter gives clear and airy highs, while twin carbon woofers deliver deep bass without boom. The cabinet is braced inside to stay silent at any volume.\n\nDecoupling feet keep the floor from colouring the sound.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Decoupling feet"] = 8, ["Speaker cable"] = 2, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06", "walnut-bookshelf-speakers", "Walnut Bookshelf Speakers",
            ProductCategory.Speakers, price: 3999m, stock: 12,
            imageUrl: "/assets/products/walnut-bookshelf-speakers.jpg",
            description: "A pair of bookshelf speakers in real walnut veneer with copper woofers and titanium tweeters, for a warm and detailed sound at home.",
            features: "Compact enough for a shelf or desk, with bass that sounds like a bigger speaker thanks to the rear port. The titanium tweeters keep vocals clear and natural.\n\nBuilt in amplifier with Bluetooth, optical and RCA inputs.",
            inTheBox: new() { ["Speaker unit"] = 2, ["Remote control"] = 1, ["Optical cable"] = 1, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07", "carbon-desk-monitors", "Carbon Desk Monitors",
            ProductCategory.Speakers, price: 2899m, stock: 16,
            imageUrl: "/assets/products/carbon-desk-monitors.jpg",
            description: "Compact desktop monitors in a matte carbon finish with exposed copper drivers, made for creators who work close to their speakers.",
            features: "Near field tuning sounds right from a short distance, so your mixes travel well to other systems. Front controls let you adjust volume and tone without reaching behind.\n\nUSB-C audio input connects straight to a laptop.",
            inTheBox: new() { ["Speaker unit"] = 2, ["USB-C cable"] = 1, ["Isolation pads"] = 2, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08", "cylinder-home-speaker", "Cylinder Home Speaker",
            ProductCategory.Speakers, price: 1899m, stock: 22,
            imageUrl: "/assets/products/cylinder-home-speaker.jpg",
            description: "A wireless speaker in woven slate fabric with a copper dial on top, filling any room with even, room wide sound.",
            features: "Drivers face all directions, so the sound is the same wherever you sit. Turn the copper dial to change the volume and tap it to pause.\n\nPair two for stereo, or stream from any phone over Bluetooth or Wi-Fi.",
            inTheBox: new() { ["Speaker unit"] = 1, ["Power cable"] = 1, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09", "yx2-pro-earphones", "YX2 Pro Earphones",
            ProductCategory.Earphones, price: 1299m, stock: 30,
            imageUrl: "/assets/products/yx2-pro-earphones.jpg",
            description: "True wireless earphones with brass acoustic chambers and active noise cancellation, our best sound in the smallest size.",
            features: "Brass chambers tame resonance for a cleaner, richer sound than plastic shells. Active noise cancellation adapts to where you are.\n\nEight hours of play, plus three more charges in the aluminum case.",
            inTheBox: new() { ["Earphone unit"] = 2, ["Charging case"] = 1, ["Ear tips"] = 6, ["USB-C cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10", "yx-sport-earphones", "YX Sport Earphones",
            ProductCategory.Earphones, price: 799m, stock: 40,
            imageUrl: "/assets/products/yx-sport-earphones.jpg",
            description: "Light, sweat resistant wireless earphones with a secure fit, made for running, training and long days on the move.",
            features: "Wing tips hold the earphones in place through any workout, and the IPX5 rating keeps out sweat and rain. Transparency mode lets you hear traffic when you need to.\n\nQuick charge gives an hour of music from ten minutes in the case.",
            inTheBox: new() { ["Earphone unit"] = 2, ["Charging case"] = 1, ["Wing tips"] = 3, ["USB-C cable"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11", "aurum-reference-dac", "Aurum Reference DAC",
            ProductCategory.Amplifiers, price: 6499m, stock: 6,
            imageUrl: "/assets/products/aurum-reference-dac.jpg",
            description: "A reference digital to analog converter and balanced headphone amplifier, machined from a solid block of aluminum.",
            features: "Dual converter chips run in mono mode, one per channel, for a black background and precise stereo image. The balanced output drives even the hardest headphones with ease.\n\nUSB, optical and coaxial inputs, with a clear display for the volume and sample rate.",
            inTheBox: new() { ["DAC unit"] = 1, ["Power supply"] = 1, ["USB cable"] = 1, ["Remote control"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12", "tube-headphone-amplifier", "Tube Headphone Amplifier",
            ProductCategory.Amplifiers, price: 4899m, stock: 7,
            imageUrl: "/assets/products/tube-headphone-amplifier.jpg",
            description: "A vacuum tube headphone amplifier with a warm amber glow, adding body and depth to every recording.",
            features: "Hand selected tubes give the smooth, rich sound tube lovers know. A stepped volume control keeps both channels perfectly matched.\n\nTwo headphone outputs and a preamp output for powered speakers.",
            inTheBox: new() { ["Amplifier unit"] = 1, ["Vacuum tubes"] = 4, ["Power cable"] = 1, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13", "desktop-dac-one", "Desktop DAC One",
            ProductCategory.Amplifiers, price: 2499m, stock: 15,
            imageUrl: "/assets/products/desktop-dac-one.jpg",
            description: "A compact DAC and headphone amplifier for your desk, the easiest way to hear more from your computer.",
            features: "Plug in over USB and it works with no drivers. The amplifier has enough power for studio headphones and a low noise floor for sensitive earphones.\n\nAn amber display shows the volume and the format you are playing.",
            inTheBox: new() { ["DAC unit"] = 1, ["USB-C cable"] = 1, ["User manual"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14", "walnut-headphone-stand", "Walnut Headphone Stand",
            ProductCategory.Accessories, price: 899m, stock: 20,
            imageUrl: "/assets/products/walnut-headphone-stand.jpg",
            description: "A curved stand in solid American walnut on a heavy aluminum base, keeping your headphones in shape and on display.",
            features: "The wide curve matches the shape of a headband so the padding never flattens. The weighted base stays steady on any desk.\n\nEach stand is shaped and oiled by hand, so the grain is unique.",
            inTheBox: new() { ["Headphone stand"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15", "silver-balanced-cable", "Silver Balanced Cable",
            ProductCategory.Accessories, price: 1199m, stock: 25,
            imageUrl: "/assets/products/silver-balanced-cable.jpg",
            description: "A braided silver plated headphone cable with a 4.4mm balanced plug, an easy upgrade for detachable headphones.",
            features: "Eight braided cores lower resistance and keep the cable soft and tangle free. The balanced plug works with modern players and amplifiers.\n\nAvailable for the XX99 and Planar Pro connectors.",
            inTheBox: new() { ["Cable"] = 1, ["Storage pouch"] = 1 }),
        Seed("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16", "velvet-replacement-earpads", "Velvet Replacement Earpads",
            ProductCategory.Accessories, price: 349m, stock: 50,
            imageUrl: "/assets/products/velvet-replacement-earpads.jpg",
            description: "Soft velvet memory foam earpads with cooling gel, bringing old headphones back to their first day comfort.",
            features: "Cooling gel keeps your ears fresh in long sessions and the velvet cover is gentle on skin. They clip onto the XX99 and Bronze Edition headphones in seconds.",
            inTheBox: new() { ["Earpads"] = 2 }));
  }

  // The three gallery photos that came with the original design
  private static string[] Gallery(string slug) =>
      [.. Enumerable.Range(1, 3).Select(n => $"/assets/product-{slug}/desktop/image-gallery-{n}.jpg")];

  private static object Seed(
      string id, string slug, string name, ProductCategory category, decimal price, int stock,
      string imageUrl, string description, string features, Dictionary<string, int> inTheBox, string[]? gallery = null) => new
      {
        Id = Guid.Parse(id),
        Name = name,
        Slug = slug,
        Description = description,
        Features = features,
        Price = price,
        Stock = stock,
        _images = new List<string> { imageUrl }.Concat(gallery ?? []).ToList(),
        IsActive = true,
        Category = category,
        CreatedAt = SeededAt,
        _inTheBox = inTheBox,
      };
}
