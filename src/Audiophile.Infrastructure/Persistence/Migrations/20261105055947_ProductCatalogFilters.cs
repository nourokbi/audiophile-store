using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalogFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4ae5911a-421b-4565-b75f-53809c3da182"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a01"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a03"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a04"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a05"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0a06"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b04"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b05"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b06"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b09"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b10"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b11"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b12"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b14"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b16"),
                column: "IsNew",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c01"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c02"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c03"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c04"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c05"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c06"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c07"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c08"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c09"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c10"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c11"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c12"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c13"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c14"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c15"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c16"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c17"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c18"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c19"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c20"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c21"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c22"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c23"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c24"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c25"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c26"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c27"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c28"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c29"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c30"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c31"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c32"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c33"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c34"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c35"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c36"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c37"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c38"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c39"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c40"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c41"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c42"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c43"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c44"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c45"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c46"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c47"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c48"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c49"),
                column: "IsNew",
                value: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0c50"),
                column: "IsNew",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_IsNew_Price_Name",
                table: "Products",
                columns: new[] { "Category", "IsNew", "Price", "Name" },
                descending: new[] { false, true, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Name",
                table: "Products",
                columns: new[] { "Category", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Price_Name",
                table: "Products",
                columns: new[] { "Category", "Price", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsNew_Price_Name",
                table: "Products",
                columns: new[] { "IsNew", "Price", "Name" },
                descending: new[] { true, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Price_Name",
                table: "Products",
                columns: new[] { "Price", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Category_IsNew_Price_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category_Price_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsNew_Price_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Price_Name",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Products");
        }
    }
}
