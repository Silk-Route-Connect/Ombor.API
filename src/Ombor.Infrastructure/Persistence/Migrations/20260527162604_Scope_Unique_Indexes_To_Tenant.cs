using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Scope_Unique_Indexes_To_Tenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_SKU",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Partner_Name",
                table: "Partner");

            migrationBuilder.CreateIndex(
                name: "IX_Product_TenantId_SKU",
                table: "Product",
                columns: new[] { "TenantId", "SKU" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partner_Name",
                table: "Partner",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_TenantId_SKU",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Partner_Name",
                table: "Partner");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SKU",
                table: "Product",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partner_Name",
                table: "Partner",
                column: "Name",
                unique: true);
        }
    }
}
