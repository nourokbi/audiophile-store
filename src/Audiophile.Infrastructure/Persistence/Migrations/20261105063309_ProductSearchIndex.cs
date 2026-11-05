using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audiophile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            // EF can't describe an index on lower(column), and these must match the lower(column) LIKE that search sends
            migrationBuilder.Sql("""CREATE INDEX "IX_Products_Name_Trigram" ON "Products" USING gin (lower("Name") gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX "IX_Products_Description_Trigram" ON "Products" USING gin (lower("Description") gin_trgm_ops);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Products_Name_Trigram";""");
            migrationBuilder.Sql("""DROP INDEX "IX_Products_Description_Trigram";""");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
