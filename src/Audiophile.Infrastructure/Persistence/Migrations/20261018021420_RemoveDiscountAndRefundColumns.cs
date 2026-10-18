using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDiscountAndRefundColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_RefundedAmount_Valid",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_DiscountAmount_Valid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_RefundedAmount_Valid",
                table: "Payments",
                sql: "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"Amount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_DiscountAmount_Valid",
                table: "Orders",
                sql: "\"DiscountAmount\" >= 0 AND \"DiscountAmount\" <= \"SubTotal\"");
        }
    }
}
