using Microsoft.EntityFrameworkCore.Migrations;
using Ombor.Infrastructure.Persistence.DataFixes;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Transaction_Line_Cost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CostIsEstimated",
                table: "TransactionLine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "TransactionLine",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql(TransactionLineCostBackfill.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostIsEstimated",
                table: "TransactionLine");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "TransactionLine");
        }
    }
}
