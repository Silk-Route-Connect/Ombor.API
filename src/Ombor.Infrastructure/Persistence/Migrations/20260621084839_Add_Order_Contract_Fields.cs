using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Order_Contract_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryAddress_Longitude",
                table: "Order",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryAddress_Latitude",
                table: "Order",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Text",
                table: "Order",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeliveryDate",
                table: "Order",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryTime",
                table: "Order",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Order",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Order",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderStatusEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    From = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    To = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    By = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusEvent_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_SaleId",
                table: "Order",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_WarehouseId",
                table: "Order",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusEvent_OrderId",
                table: "OrderStatusEvent",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusEvent_OrganizationId",
                table: "OrderStatusEvent",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Inventory_WarehouseId",
                table: "Order",
                column: "WarehouseId",
                principalTable: "Inventory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_TransactionRecord_SaleId",
                table: "Order",
                column: "SaleId",
                principalTable: "TransactionRecord",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Inventory_WarehouseId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_TransactionRecord_SaleId",
                table: "Order");

            migrationBuilder.DropTable(
                name: "OrderStatusEvent");

            migrationBuilder.DropIndex(
                name: "IX_Order_SaleId",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Order_WarehouseId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Text",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryTime",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Order");

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryAddress_Longitude",
                table: "Order",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryAddress_Latitude",
                table: "Order",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);
        }
    }
}
