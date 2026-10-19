using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedNewCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Features", "ImageUrl", "IsActive", "Name", "Price", "Slug", "Stock", "InTheBox" },
                values: new object[,]
                {
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01"), "Headphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Closed-back studio headphones with a lambskin leather headband and machined brass hinges, tuned flat for mixing and long listening sessions.", "50mm dynamic drivers with a neutral tuning that tells you what is really in the recording. Memory foam earcups seal out the room without pressing on your ears.\n\nThe brass hinges fold flat for travel, and the detachable cable locks into place so it never slips out mid session.", "/assets/products/studio-reference-headphones.jpg", true, "Studio Reference Headphones", 3299m, "studio-reference-headphones", 18, "{\"Headphone unit\":1,\"Detachable 3m cable\":1,\"6.35mm adapter\":1,\"Carry case\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02"), "Headphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Open-back planar magnetic headphones with brushed copper details, made for listeners who want every texture of the music.", "Planar magnetic drivers move the whole diaphragm at once, giving fast and clean detail with very low distortion. The open back creates a wide and natural soundstage.\n\nA light aluminum frame and suspended headband spread the weight, so they stay comfortable for hours.", "/assets/products/planar-pro-headphones.jpg", true, "Planar Pro Headphones", 5299m, "planar-pro-headphones", 8, "{\"Headphone unit\":1,\"Balanced 4.4mm cable\":1,\"3.5mm cable\":1,\"Hard case\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03"), "Headphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Matte black over-ear headphones with polished bronze arms and plush leather earcups, a limited edition of our classic sound.", "Warm and full bodied tuning with a deep, controlled bass. The bronze arms are machined from solid metal and finished by hand.\n\nWireless with 30 hours of battery, or wired when you want the purest signal.", "/assets/products/bronze-edition-headphones.jpg", true, "Bronze Edition Headphones", 2799m, "bronze-edition-headphones", 14, "{\"Headphone unit\":1,\"USB-C charging cable\":1,\"3.5mm cable\":1,\"Travel pouch\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04"), "Headphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Everyday monitor headphones with lambskin earcups and a braided copper cable, ready for the studio desk or the commute.", "Closed-back design keeps your music in and the noise out. A balanced tuning works for recording, editing and simply enjoying music.\n\nThe braided cable resists tangles and the earcups can be replaced when they wear.", "/assets/products/closed-back-monitor-headphones.jpg", true, "Closed-Back Monitor Headphones", 2199m, "closed-back-monitor-headphones", 25, "{\"Headphone unit\":1,\"Braided cable\":1,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05"), "Speakers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A tall floorstanding speaker with a beryllium dome tweeter and carbon weave woofers, built to fill a large room.", "The beryllium tweeter gives clear and airy highs, while twin carbon woofers deliver deep bass without boom. The cabinet is braced inside to stay silent at any volume.\n\nDecoupling feet keep the floor from colouring the sound.", "/assets/products/zx12-tower-speaker.jpg", true, "ZX12 Tower Speaker", 8499m, "zx12-tower-speaker", 4, "{\"Speaker unit\":2,\"Decoupling feet\":8,\"Speaker cable\":2,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06"), "Speakers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A pair of bookshelf speakers in real walnut veneer with copper woofers and titanium tweeters, for a warm and detailed sound at home.", "Compact enough for a shelf or desk, with bass that sounds like a bigger speaker thanks to the rear port. The titanium tweeters keep vocals clear and natural.\n\nBuilt in amplifier with Bluetooth, optical and RCA inputs.", "/assets/products/walnut-bookshelf-speakers.jpg", true, "Walnut Bookshelf Speakers", 3999m, "walnut-bookshelf-speakers", 12, "{\"Speaker unit\":2,\"Remote control\":1,\"Optical cable\":1,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07"), "Speakers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Compact desktop monitors in a matte carbon finish with exposed copper drivers, made for creators who work close to their speakers.", "Near field tuning sounds right from a short distance, so your mixes travel well to other systems. Front controls let you adjust volume and tone without reaching behind.\n\nUSB-C audio input connects straight to a laptop.", "/assets/products/carbon-desk-monitors.jpg", true, "Carbon Desk Monitors", 2899m, "carbon-desk-monitors", 16, "{\"Speaker unit\":2,\"USB-C cable\":1,\"Isolation pads\":2,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08"), "Speakers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A wireless speaker in woven slate fabric with a copper dial on top, filling any room with even, room wide sound.", "Drivers face all directions, so the sound is the same wherever you sit. Turn the copper dial to change the volume and tap it to pause.\n\nPair two for stereo, or stream from any phone over Bluetooth or Wi-Fi.", "/assets/products/cylinder-home-speaker.jpg", true, "Cylinder Home Speaker", 1899m, "cylinder-home-speaker", 22, "{\"Speaker unit\":1,\"Power cable\":1,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09"), "Earphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "True wireless earphones with brass acoustic chambers and active noise cancellation, our best sound in the smallest size.", "Brass chambers tame resonance for a cleaner, richer sound than plastic shells. Active noise cancellation adapts to where you are.\n\nEight hours of play, plus three more charges in the aluminum case.", "/assets/products/yx2-pro-earphones.jpg", true, "YX2 Pro Earphones", 1299m, "yx2-pro-earphones", 30, "{\"Earphone unit\":2,\"Charging case\":1,\"Ear tips\":6,\"USB-C cable\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10"), "Earphones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Light, sweat resistant wireless earphones with a secure fit, made for running, training and long days on the move.", "Wing tips hold the earphones in place through any workout, and the IPX5 rating keeps out sweat and rain. Transparency mode lets you hear traffic when you need to.\n\nQuick charge gives an hour of music from ten minutes in the case.", "/assets/products/yx-sport-earphones.jpg", true, "YX Sport Earphones", 799m, "yx-sport-earphones", 40, "{\"Earphone unit\":2,\"Charging case\":1,\"Wing tips\":3,\"USB-C cable\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11"), "Amplifiers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A reference digital to analog converter and balanced headphone amplifier, machined from a solid block of aluminum.", "Dual converter chips run in mono mode, one per channel, for a black background and precise stereo image. The balanced output drives even the hardest headphones with ease.\n\nUSB, optical and coaxial inputs, with a clear display for the volume and sample rate.", "/assets/products/aurum-reference-dac.jpg", true, "Aurum Reference DAC", 6499m, "aurum-reference-dac", 6, "{\"DAC unit\":1,\"Power supply\":1,\"USB cable\":1,\"Remote control\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12"), "Amplifiers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A vacuum tube headphone amplifier with a warm amber glow, adding body and depth to every recording.", "Hand selected tubes give the smooth, rich sound tube lovers know. A stepped volume control keeps both channels perfectly matched.\n\nTwo headphone outputs and a preamp output for powered speakers.", "/assets/products/tube-headphone-amplifier.jpg", true, "Tube Headphone Amplifier", 4899m, "tube-headphone-amplifier", 7, "{\"Amplifier unit\":1,\"Vacuum tubes\":4,\"Power cable\":1,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13"), "Amplifiers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A compact DAC and headphone amplifier for your desk, the easiest way to hear more from your computer.", "Plug in over USB and it works with no drivers. The amplifier has enough power for studio headphones and a low noise floor for sensitive earphones.\n\nAn amber display shows the volume and the format you are playing.", "/assets/products/desktop-dac-one.jpg", true, "Desktop DAC One", 2499m, "desktop-dac-one", 15, "{\"DAC unit\":1,\"USB-C cable\":1,\"User manual\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14"), "Accessories", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A curved stand in solid American walnut on a heavy aluminum base, keeping your headphones in shape and on display.", "The wide curve matches the shape of a headband so the padding never flattens. The weighted base stays steady on any desk.\n\nEach stand is shaped and oiled by hand, so the grain is unique.", "/assets/products/walnut-headphone-stand.jpg", true, "Walnut Headphone Stand", 899m, "walnut-headphone-stand", 20, "{\"Headphone stand\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15"), "Accessories", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A braided silver plated headphone cable with a 4.4mm balanced plug, an easy upgrade for detachable headphones.", "Eight braided cores lower resistance and keep the cable soft and tangle free. The balanced plug works with modern players and amplifiers.\n\nAvailable for the XX99 and Planar Pro connectors.", "/assets/products/silver-balanced-cable.jpg", true, "Silver Balanced Cable", 1199m, "silver-balanced-cable", 25, "{\"Cable\":1,\"Storage pouch\":1}" },
                    { new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16"), "Accessories", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Soft velvet memory foam earpads with cooling gel, bringing old headphones back to their first day comfort.", "Cooling gel keeps your ears fresh in long sessions and the velvet cover is gentle on skin. They clip onto the XX99 and Bronze Edition headphones in seconds.", "/assets/products/velvet-replacement-earpads.jpg", true, "Velvet Replacement Earpads", 349m, "velvet-replacement-earpads", 50, "{\"Earpads\":2}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16"));
        }
    }
}
