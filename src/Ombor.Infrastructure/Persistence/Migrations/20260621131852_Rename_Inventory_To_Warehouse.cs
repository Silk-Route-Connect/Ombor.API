using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rename_Inventory_To_Warehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Non-destructive rename of the stock-location model: Inventory→Warehouse (the place),
            // InventoryItem→WarehouseItem (its stock), and every InventoryId foreign key→WarehouseId.
            // EF scaffolds a drop/create here (which would lose every warehouse + all stock); it is
            // replaced with sp_rename-backed renames. The old IsActive flag is also folded into the new
            // IsArchived flag (archived = not active).
            migrationBuilder.RenameTable(name: "Inventory", newName: "Warehouse");
            migrationBuilder.RenameTable(name: "InventoryItem", newName: "WarehouseItem");

            migrationBuilder.Sql("EXEC sp_rename N'PK_Inventory', N'PK_Warehouse', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'PK_InventoryItem', N'PK_WarehouseItem', N'OBJECT';");

            migrationBuilder.Sql("EXEC sp_rename N'FK_InventoryItem_Inventory_InventoryId', N'FK_WarehouseItem_Warehouse_WarehouseId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_InventoryItem_Product_ProductId', N'FK_WarehouseItem_Product_ProductId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Order_Inventory_WarehouseId', N'FK_Order_Warehouse_WarehouseId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_TransactionRecord_Inventory_InventoryId', N'FK_TransactionRecord_Warehouse_WarehouseId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Transfer_Inventory_FromInventoryId', N'FK_Transfer_Warehouse_FromWarehouseId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Transfer_Inventory_ToInventoryId', N'FK_Transfer_Warehouse_ToWarehouseId', N'OBJECT';");

            migrationBuilder.RenameColumn(name: "InventoryId", table: "WarehouseItem", newName: "WarehouseId");
            migrationBuilder.RenameColumn(name: "InventoryId", table: "TransactionRecord", newName: "WarehouseId");
            migrationBuilder.RenameColumn(name: "FromInventoryId", table: "Transfer", newName: "FromWarehouseId");
            migrationBuilder.RenameColumn(name: "ToInventoryId", table: "Transfer", newName: "ToWarehouseId");
            migrationBuilder.RenameColumn(name: "IsActive", table: "Warehouse", newName: "IsArchived");

            migrationBuilder.RenameIndex(name: "IX_Inventory_OrganizationId", table: "Warehouse", newName: "IX_Warehouse_OrganizationId");
            migrationBuilder.RenameIndex(name: "IX_InventoryItem_OrganizationId", table: "WarehouseItem", newName: "IX_WarehouseItem_OrganizationId");
            migrationBuilder.RenameIndex(name: "IX_InventoryItem_ProductId", table: "WarehouseItem", newName: "IX_WarehouseItem_ProductId");
            migrationBuilder.RenameIndex(name: "IX_InventoryItem_InventoryId_ProductId", table: "WarehouseItem", newName: "IX_WarehouseItem_WarehouseId_ProductId");
            migrationBuilder.RenameIndex(name: "IX_TransactionRecord_InventoryId", table: "TransactionRecord", newName: "IX_TransactionRecord_WarehouseId");
            migrationBuilder.RenameIndex(name: "IX_Transfer_FromInventoryId", table: "Transfer", newName: "IX_Transfer_FromWarehouseId");
            migrationBuilder.RenameIndex(name: "IX_Transfer_ToInventoryId", table: "Transfer", newName: "IX_Transfer_ToWarehouseId");

            // Old "active" → new "archived" (archived = not active).
            migrationBuilder.Sql("UPDATE [Warehouse] SET [IsArchived] = CASE WHEN [IsArchived] = 1 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore "active" from "archived", then rename everything back.
            migrationBuilder.Sql("UPDATE [Warehouse] SET [IsArchived] = CASE WHEN [IsArchived] = 1 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END;");

            migrationBuilder.RenameIndex(name: "IX_Transfer_ToWarehouseId", table: "Transfer", newName: "IX_Transfer_ToInventoryId");
            migrationBuilder.RenameIndex(name: "IX_Transfer_FromWarehouseId", table: "Transfer", newName: "IX_Transfer_FromInventoryId");
            migrationBuilder.RenameIndex(name: "IX_TransactionRecord_WarehouseId", table: "TransactionRecord", newName: "IX_TransactionRecord_InventoryId");
            migrationBuilder.RenameIndex(name: "IX_WarehouseItem_WarehouseId_ProductId", table: "WarehouseItem", newName: "IX_InventoryItem_InventoryId_ProductId");
            migrationBuilder.RenameIndex(name: "IX_WarehouseItem_ProductId", table: "WarehouseItem", newName: "IX_InventoryItem_ProductId");
            migrationBuilder.RenameIndex(name: "IX_WarehouseItem_OrganizationId", table: "WarehouseItem", newName: "IX_InventoryItem_OrganizationId");
            migrationBuilder.RenameIndex(name: "IX_Warehouse_OrganizationId", table: "Warehouse", newName: "IX_Inventory_OrganizationId");

            migrationBuilder.RenameColumn(name: "IsArchived", table: "Warehouse", newName: "IsActive");
            migrationBuilder.RenameColumn(name: "ToWarehouseId", table: "Transfer", newName: "ToInventoryId");
            migrationBuilder.RenameColumn(name: "FromWarehouseId", table: "Transfer", newName: "FromInventoryId");
            migrationBuilder.RenameColumn(name: "WarehouseId", table: "TransactionRecord", newName: "InventoryId");
            migrationBuilder.RenameColumn(name: "WarehouseId", table: "WarehouseItem", newName: "InventoryId");

            migrationBuilder.Sql("EXEC sp_rename N'FK_Transfer_Warehouse_ToWarehouseId', N'FK_Transfer_Inventory_ToInventoryId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Transfer_Warehouse_FromWarehouseId', N'FK_Transfer_Inventory_FromInventoryId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_TransactionRecord_Warehouse_WarehouseId', N'FK_TransactionRecord_Inventory_InventoryId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Order_Warehouse_WarehouseId', N'FK_Order_Inventory_WarehouseId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_WarehouseItem_Product_ProductId', N'FK_InventoryItem_Product_ProductId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_WarehouseItem_Warehouse_WarehouseId', N'FK_InventoryItem_Inventory_InventoryId', N'OBJECT';");

            migrationBuilder.Sql("EXEC sp_rename N'PK_WarehouseItem', N'PK_InventoryItem', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'PK_Warehouse', N'PK_Inventory', N'OBJECT';");

            migrationBuilder.RenameTable(name: "WarehouseItem", newName: "InventoryItem");
            migrationBuilder.RenameTable(name: "Warehouse", newName: "Inventory");
        }
    }
}
