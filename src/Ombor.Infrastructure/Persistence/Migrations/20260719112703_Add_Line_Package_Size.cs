using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Line_Package_Size : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackageSize",
                table: "TransactionLine",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageSize",
                table: "TemplateItem",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "TransactionLine");

            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "TemplateItem");
        }
    }
}
