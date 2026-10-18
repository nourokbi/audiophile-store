using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStorefrontProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4ae5911a-421b-4565-b75f-53809c3da182"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "Slug", "InTheBox" },
                values: new object[] { "Enjoy your audio almost anywhere and customize it to your specific tastes with the XX59 headphones. The stylish yet durable versatile wireless headset is a brilliant companion at home or on the move.", "These headphones have been created from durable, high-quality materials tough enough to take anywhere. Its compact folding design fuses comfort and minimalist style making it perfect for travel. Flawless transmission is assured by the latest wireless technology engineered for audio synchronization with videos.\n\nMore than a simple pair of headphones, this headset features a pair of built-in microphones for clear, hands-free calling when paired with a compatible smartphone. Controlling music and calls is also intuitive thanks to easy-access touch buttons on the earcups. Regardless of how you use the XX59 headphones, you can do so all day thanks to an impressive 30-hour battery life that can be rapidly recharged via USB-C.", "/assets/cart/image-xx59-headphones.jpg", 899m, "xx59-headphones", "{\"Headphone unit\":1,\"Replacement earcups\":2,\"User manual\":1,\"3.5mm 5m audio cable\":1}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "Slug", "InTheBox" },
                values: new object[] { "The new XX99 Mark II headphones is the pinnacle of pristine audio. It redefines your premium headphone experience by reproducing the balanced depth and precision of studio-quality sound.", "Featuring a genuine leather head strap and premium earcups, these headphones deliver superior comfort for those who like to enjoy endless listening. It includes intuitive controls designed for any situation. Whether you’re taking a business call or just in your own personal space, the auto on/off and pause features ensure that you’ll never miss a beat.\n\nThe advanced Active Noise Cancellation with built-in equalizer allow you to experience your audio world on your terms. It lets you enjoy your audio in peace, but quickly interact with your surroundings when you need to. Combined with Bluetooth 5. 0 compliant connectivity and 17 hour battery life, the XX99 Mark II headphones gives you superior sound, cutting-edge technology, and a modern design aesthetic.", "/assets/cart/image-xx99-mark-two-headphones.jpg", 2999m, "xx99-mark-two-headphones", "{\"Headphone unit\":1,\"Replacement earcups\":2,\"User manual\":1,\"3.5mm 5m audio cable\":1,\"Travel bag\":1}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "Slug", "InTheBox" },
                values: new object[] { "Upgrade your sound system with the all new ZX9 active speaker. It’s a bookshelf speaker system that offers truly wireless connectivity -- creating new possibilities for more pleasing and practical audio setups.", "Connect via Bluetooth or nearly any wired source. This speaker features optical, digital coaxial, USB Type-B, stereo RCA, and stereo XLR inputs, allowing you to have up to five wired source devices connected for easy switching. Improved bluetooth technology offers near lossless audio quality at up to 328ft (100m).\n\nDiscover clear, more natural sounding highs than the competition with ZX9’s signature planar diaphragm tweeter. Equally important is its powerful room-shaking bass courtesy of a 6.5” aluminum alloy bass unit. You’ll be able to enjoy equal sound quality whether in a large room or small den. Furthermore, you will experience new sensations from old songs since it can respond to even the subtle waveforms.", "/assets/cart/image-zx9-speaker.jpg", 4500m, "zx9-speaker", "{\"Speaker unit\":2,\"Speaker cloth panel\":2,\"User manual\":1,\"3.5mm 10m audio cable\":1,\"10m optical cable\":1}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "Slug", "Stock", "InTheBox" },
                values: new object[] { "Tailor your listening experience with bespoke dynamic drivers from the new YX1 Wireless Earphones. Enjoy incredible high-fidelity sound even in noisy environments with its active noise cancellation feature.", "Experience unrivalled stereo sound thanks to innovative acoustic technology. With improved ergonomics designed for full day wearing, these revolutionary earphones have been finely crafted to provide you with the perfect fit, delivering complete comfort all day long while enjoying exceptional noise isolation and truly immersive sound.\n\nThe YX1 Wireless Earphones features customizable controls for volume, music, calls, and voice assistants built into both earbuds. The new 7-hour battery life can be extended up to 28 hours with the charging case, giving you uninterrupted play time. Exquisite craftsmanship with a splash resistant design now available in an all new white and grey color scheme as well as the popular classic black.", "/assets/cart/image-yx1-earphones.jpg", 599m, "yx1-earphones", 30, "{\"Earphone unit\":2,\"Multi-size earplugs\":6,\"User manual\":1,\"USB-C charging cable\":1,\"Travel pouch\":1}" });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Features", "ImageUrl", "IsActive", "Name", "Price", "Slug", "Stock", "InTheBox" },
                values: new object[,]
                {
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05"), "Headphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "As the gold standard for headphones, the classic XX99 Mark I offers detailed and accurate audio reproduction for audiophiles, mixing engineers, and music aficionados alike in studios and on the go.", "As the headphones all others are measured against, the XX99 Mark I demonstrates over five decades of audio expertise, redefining the critical listening experience. This pair of closed-back headphones are made of industrial, aerospace-grade materials to emphasize durability at a relatively light weight of 11 oz.\n\nFrom the handcrafted microfiber ear cushions to the robust metal headband with inner damping element, the components work together to deliver comfort and uncompromising sound. Its closed-back design delivers up to 27 dB of passive noise cancellation, reducing resonance by reflecting sound to a dedicated absorber. For connectivity, a specially tuned cable is includes with a balanced gold connector.", "/assets/cart/image-xx99-mark-one-headphones.jpg", true, "XX99 Mark I Headphones", 1750m, "xx99-mark-one-headphones", 20, "{\"Headphone unit\":1,\"Replacement earcups\":2,\"User manual\":1,\"3.5mm 5m audio cable\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06"), "Speakers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Stream high quality sound wirelessly with minimal to no loss. The ZX7 speaker uses high-end audiophile components that represents the top of the line powered speakers for home or studio use.", "Reap the advantages of a flat diaphragm tweeter cone. This provides a fast response rate and excellent high frequencies that lower tiered bookshelf speakers cannot provide. The woofers are made from aluminum that produces a unique and clear sound. XLR inputs allow you to connect to a mixer for more advanced usage.\n\nThe ZX7 speaker is the perfect blend of stylish design and high performance. It houses an encased MDF wooden enclosure which minimises acoustic resonance. Dual connectivity allows pairing through bluetooth or traditional optical and RCA input. Switch input sources and control volume at your finger tips with the included wireless remote. This versatile speaker is equipped to deliver an authentic listening experience.", "/assets/cart/image-zx7-speaker.jpg", true, "ZX7 Speaker", 3500m, "zx7-speaker", 15, "{\"Speaker unit\":2,\"Speaker cloth panel\":2,\"User manual\":1,\"3.5mm 7.5m audio cable\":1,\"7.5m optical cable\":1}" }
                });

            // Products that aren't seeded (e.g. ones an admin added) got an empty slug from
            // AddColumn. Give each one from its name, plus part of its id so two can't clash,
            // before the unique index is created.
            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "Slug" = trim(both '-' from regexp_replace(lower("Name"), '[^a-z0-9]+', '-', 'g'))
                             || '-' || left("Id"::text, 8)
                WHERE "Slug" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06"));

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4ae5911a-421b-4565-b75f-53809c3da182"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "InTheBox" },
                values: new object[] { "Enjoy your audio almost anywhere and customize it to your specific tastes.", "", null, 4499m, "{}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "InTheBox" },
                values: new object[] { "The new XX99 Mark II headphones is the pinnacle of pristine audio.", "", null, 14999m, "{}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "InTheBox" },
                values: new object[] { "Upgrade your sound system with the all new ZX9 active speaker.", "", null, 22500m, "{}" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04"),
                columns: new[] { "Description", "Features", "ImageUrl", "Price", "Stock", "InTheBox" },
                values: new object[] { "Tailor your listening experience with bespoke dynamic drivers.", "", null, 2999m, 3, "{}" });
        }
    }
}
