using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Product_Packaging : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Packaging_Barcode",
            table: "Product",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Packaging_Label",
            table: "Product",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Packaging_Size",
            table: "Product",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Packaging_Barcode",
            table: "Product");

        migrationBuilder.DropColumn(
            name: "Packaging_Label",
            table: "Product");

        migrationBuilder.DropColumn(
            name: "Packaging_Size",
            table: "Product");
    }
}
