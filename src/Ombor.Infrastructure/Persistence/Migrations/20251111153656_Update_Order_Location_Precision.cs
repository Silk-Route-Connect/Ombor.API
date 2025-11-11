using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Update_Order_Location_Precision : Migration
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
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");

        migrationBuilder.AlterColumn<decimal>(
            name: "DeliveryAddress_Latitude",
            table: "Order",
            type: "decimal(9,6)",
            precision: 9,
            scale: 6,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "DeliveryAddress_Longitude",
            table: "Order",
            type: "decimal(18,2)",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(9,6)",
            oldPrecision: 9,
            oldScale: 6);

        migrationBuilder.AlterColumn<decimal>(
            name: "DeliveryAddress_Latitude",
            table: "Order",
            type: "decimal(18,2)",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(9,6)",
            oldPrecision: 9,
            oldScale: 6);
    }
}
