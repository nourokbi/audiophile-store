using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "Products",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // Products added in the dashboard keep their one photo as the first of the list
            migrationBuilder.Sql(
                "UPDATE \"Products\" SET \"Images\" = jsonb_build_array(\"ImageUrl\") WHERE \"ImageUrl\" IS NOT NULL AND \"ImageUrl\" <> '';");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4ae5911a-421b-4565-b75f-53809c3da182"),
                column: "Images",
                value: "[\"/assets/cart/image-xx59-headphones.jpg\",\"/assets/product-xx59-headphones/desktop/image-gallery-1.jpg\",\"/assets/product-xx59-headphones/desktop/image-gallery-2.jpg\",\"/assets/product-xx59-headphones/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01"),
                column: "Images",
                value: "[\"/assets/cart/image-xx99-mark-two-headphones.jpg\",\"/assets/product-xx99-mark-two-headphones/desktop/image-gallery-1.jpg\",\"/assets/product-xx99-mark-two-headphones/desktop/image-gallery-2.jpg\",\"/assets/product-xx99-mark-two-headphones/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03"),
                column: "Images",
                value: "[\"/assets/cart/image-zx9-speaker.jpg\",\"/assets/product-zx9-speaker/desktop/image-gallery-1.jpg\",\"/assets/product-zx9-speaker/desktop/image-gallery-2.jpg\",\"/assets/product-zx9-speaker/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04"),
                column: "Images",
                value: "[\"/assets/cart/image-yx1-earphones.jpg\",\"/assets/product-yx1-earphones/desktop/image-gallery-1.jpg\",\"/assets/product-yx1-earphones/desktop/image-gallery-2.jpg\",\"/assets/product-yx1-earphones/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05"),
                column: "Images",
                value: "[\"/assets/cart/image-xx99-mark-one-headphones.jpg\",\"/assets/product-xx99-mark-one-headphones/desktop/image-gallery-1.jpg\",\"/assets/product-xx99-mark-one-headphones/desktop/image-gallery-2.jpg\",\"/assets/product-xx99-mark-one-headphones/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06"),
                column: "Images",
                value: "[\"/assets/cart/image-zx7-speaker.jpg\",\"/assets/product-zx7-speaker/desktop/image-gallery-1.jpg\",\"/assets/product-zx7-speaker/desktop/image-gallery-2.jpg\",\"/assets/product-zx7-speaker/desktop/image-gallery-3.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01"),
                column: "Images",
                value: "[\"/assets/products/studio-reference-headphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02"),
                column: "Images",
                value: "[\"/assets/products/planar-pro-headphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03"),
                column: "Images",
                value: "[\"/assets/products/bronze-edition-headphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04"),
                column: "Images",
                value: "[\"/assets/products/closed-back-monitor-headphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05"),
                column: "Images",
                value: "[\"/assets/products/zx12-tower-speaker.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06"),
                column: "Images",
                value: "[\"/assets/products/walnut-bookshelf-speakers.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07"),
                column: "Images",
                value: "[\"/assets/products/carbon-desk-monitors.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08"),
                column: "Images",
                value: "[\"/assets/products/cylinder-home-speaker.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09"),
                column: "Images",
                value: "[\"/assets/products/yx2-pro-earphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10"),
                column: "Images",
                value: "[\"/assets/products/yx-sport-earphones.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11"),
                column: "Images",
                value: "[\"/assets/products/aurum-reference-dac.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12"),
                column: "Images",
                value: "[\"/assets/products/tube-headphone-amplifier.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13"),
                column: "Images",
                value: "[\"/assets/products/desktop-dac-one.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14"),
                column: "Images",
                value: "[\"/assets/products/walnut-headphone-stand.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15"),
                column: "Images",
                value: "[\"/assets/products/silver-balanced-cable.jpg\"]");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16"),
                column: "Images",
                value: "[\"/assets/products/velvet-replacement-earpads.jpg\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            // Going back keeps only the cover
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = \"Images\" ->> 0;");

            migrationBuilder.DropColumn(
                name: "Images",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4ae5911a-421b-4565-b75f-53809c3da182"),
                column: "ImageUrl",
                value: "/assets/cart/image-xx59-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01"),
                column: "ImageUrl",
                value: "/assets/cart/image-xx99-mark-two-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03"),
                column: "ImageUrl",
                value: "/assets/cart/image-zx9-speaker.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04"),
                column: "ImageUrl",
                value: "/assets/cart/image-yx1-earphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05"),
                column: "ImageUrl",
                value: "/assets/cart/image-xx99-mark-one-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06"),
                column: "ImageUrl",
                value: "/assets/cart/image-zx7-speaker.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01"),
                column: "ImageUrl",
                value: "/assets/products/studio-reference-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02"),
                column: "ImageUrl",
                value: "/assets/products/planar-pro-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03"),
                column: "ImageUrl",
                value: "/assets/products/bronze-edition-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04"),
                column: "ImageUrl",
                value: "/assets/products/closed-back-monitor-headphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05"),
                column: "ImageUrl",
                value: "/assets/products/zx12-tower-speaker.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06"),
                column: "ImageUrl",
                value: "/assets/products/walnut-bookshelf-speakers.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07"),
                column: "ImageUrl",
                value: "/assets/products/carbon-desk-monitors.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08"),
                column: "ImageUrl",
                value: "/assets/products/cylinder-home-speaker.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09"),
                column: "ImageUrl",
                value: "/assets/products/yx2-pro-earphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10"),
                column: "ImageUrl",
                value: "/assets/products/yx-sport-earphones.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11"),
                column: "ImageUrl",
                value: "/assets/products/aurum-reference-dac.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12"),
                column: "ImageUrl",
                value: "/assets/products/tube-headphone-amplifier.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13"),
                column: "ImageUrl",
                value: "/assets/products/desktop-dac-one.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14"),
                column: "ImageUrl",
                value: "/assets/products/walnut-headphone-stand.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15"),
                column: "ImageUrl",
                value: "/assets/products/silver-balanced-cable.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16"),
                column: "ImageUrl",
                value: "/assets/products/velvet-replacement-earpads.jpg");
        }
    }
}
