using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Transaction : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TransactionRecord",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TotalDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TotalPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                DateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PartnerId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionRecord", x => x.Id);
                table.ForeignKey(
                    name: "FK_TransactionRecord_Partner_PartnerId",
                    column: x => x.PartnerId,
                    principalTable: "Partner",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TransactionLine",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                TransactionId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionLine", x => x.Id);
                table.ForeignKey(
                    name: "FK_TransactionLine_Product_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Product",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TransactionLine_TransactionRecord_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "TransactionRecord",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TransactionLine_ProductId",
            table: "TransactionLine",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionLine_TransactionId",
            table: "TransactionLine",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionRecord_PartnerId",
            table: "TransactionRecord",
            column: "PartnerId");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionRecord_Status",
            table: "TransactionRecord",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TransactionLine");

        migrationBuilder.DropTable(
            name: "TransactionRecord");
    }
}
