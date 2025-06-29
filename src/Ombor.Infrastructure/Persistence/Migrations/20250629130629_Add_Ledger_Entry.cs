using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Ledger_Entry : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LedgerEntry",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                AmountLocal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Type = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                Source = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                SourceId = table.Column<int>(type: "int", nullable: false),
                PartnerId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerEntry", x => x.Id);
                table.ForeignKey(
                    name: "FK_LedgerEntry_Partner_PartnerId",
                    column: x => x.PartnerId,
                    principalTable: "Partner",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LedgerEntry_PartnerId_CreatedAtUtc",
            table: "LedgerEntry",
            columns: new[] { "PartnerId", "CreatedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LedgerEntry");
    }
}
