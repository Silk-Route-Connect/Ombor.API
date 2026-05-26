using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rename_IsDeleted_To_IsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Product",
                newName: "IsArchived");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Partner",
                newName: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Product",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Partner",
                newName: "IsDeleted");
        }
    }
}
