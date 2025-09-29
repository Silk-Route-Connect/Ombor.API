using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Inventory_Item_Product_Index : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_InventoryItem_InventoryId",
            table: "InventoryItem");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryItem_InventoryId_ProductId",
            table: "InventoryItem",
            columns: new[] { "InventoryId", "ProductId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_InventoryItem_InventoryId_ProductId",
            table: "InventoryItem");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryItem_InventoryId",
            table: "InventoryItem",
            column: "InventoryId");
    }
}
