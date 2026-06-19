using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Line_DiscountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "TransactionLine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Percentage");

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "TemplateItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Fixed");

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "OrderLine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Fixed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "TransactionLine");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "TemplateItem");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "OrderLine");
        }
    }
}
