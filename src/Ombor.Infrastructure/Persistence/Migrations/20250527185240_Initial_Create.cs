using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Category",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Category", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Product",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                SKU = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Barcode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                SupplyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                QuantityInStock = table.Column<int>(type: "int", nullable: false),
                LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                Measurement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Product", x => x.Id);
                table.ForeignKey(
                    name: "FK_Product_Category_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Category",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProductImage",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ImageName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                ThumnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ProductId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductImage", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductImage_Product_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Product",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Product_CategoryId",
            table: "Product",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Product_SKU",
            table: "Product",
            column: "SKU",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductImage_ProductId",
            table: "ProductImage",
            column: "ProductId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProductImage");

        migrationBuilder.DropTable(
            name: "Product");

        migrationBuilder.DropTable(
            name: "Category");
    }
}
