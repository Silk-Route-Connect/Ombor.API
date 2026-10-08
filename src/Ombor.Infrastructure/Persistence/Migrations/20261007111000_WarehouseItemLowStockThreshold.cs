using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// DR-41: the low-stock threshold moves from the product to the warehouse item (one product in one warehouse). A
    /// product threshold above zero is copied to every warehouse item of that product; zero or unset becomes «not
    /// tracked» (null) — with the old rule a threshold of 0 only meant «out of stock», which a row now shows on its own.
    /// </summary>
    public partial class WarehouseItemLowStockThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LowStockThreshold",
                table: "WarehouseItem",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE item
                SET item.LowStockThreshold = product.LowStockThreshold
                FROM WarehouseItem AS item
                INNER JOIN Product AS product ON product.Id = item.ProductId
                WHERE product.LowStockThreshold > 0;
                """);

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // One number per product again: the highest of its warehouse thresholds, rounded up to whole units.
            migrationBuilder.Sql("""
                UPDATE product
                SET product.LowStockThreshold = item.Threshold
                FROM Product AS product
                INNER JOIN (
                    SELECT ProductId, CAST(CEILING(CASE WHEN MAX(LowStockThreshold) > 2147483647 THEN 2147483647 ELSE MAX(LowStockThreshold) END) AS int) AS Threshold
                    FROM WarehouseItem
                    WHERE LowStockThreshold IS NOT NULL
                    GROUP BY ProductId
                ) AS item ON item.ProductId = product.Id;
                """);

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "WarehouseItem");
        }
    }
}
