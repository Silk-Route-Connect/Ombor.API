using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Audit_Transfer_And_Transaction_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventoryId",
                table: "TransactionRecord",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalTransactionId",
                table: "TransactionRecord",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Partner",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                table: "InventoryItem",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AuditEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transfer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FromInventoryId = table.Column<int>(type: "int", nullable: false),
                    ToInventoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfer_Inventory_FromInventoryId",
                        column: x => x.FromInventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfer_Inventory_ToInventoryId",
                        column: x => x.ToInventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    TransferId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferLine_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferLine_Transfer_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_InventoryId",
                table: "TransactionRecord",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_OriginalTransactionId",
                table: "TransactionRecord",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_EntityType_EntityId",
                table: "AuditEntry",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_TenantId",
                table: "AuditEntry",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_FromInventoryId",
                table: "Transfer",
                column: "FromInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_TenantId",
                table: "Transfer",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_ToInventoryId",
                table: "Transfer",
                column: "ToInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLine_ProductId",
                table: "TransferLine",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLine_TenantId",
                table: "TransferLine",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLine_TransferId",
                table: "TransferLine",
                column: "TransferId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionRecord_Inventory_InventoryId",
                table: "TransactionRecord",
                column: "InventoryId",
                principalTable: "Inventory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionRecord_TransactionRecord_OriginalTransactionId",
                table: "TransactionRecord",
                column: "OriginalTransactionId",
                principalTable: "TransactionRecord",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionRecord_Inventory_InventoryId",
                table: "TransactionRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionRecord_TransactionRecord_OriginalTransactionId",
                table: "TransactionRecord");

            migrationBuilder.DropTable(
                name: "AuditEntry");

            migrationBuilder.DropTable(
                name: "TransferLine");

            migrationBuilder.DropTable(
                name: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_TransactionRecord_InventoryId",
                table: "TransactionRecord");

            migrationBuilder.DropIndex(
                name: "IX_TransactionRecord_OriginalTransactionId",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "OriginalTransactionId",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "AverageCost",
                table: "InventoryItem");
        }
    }
}
