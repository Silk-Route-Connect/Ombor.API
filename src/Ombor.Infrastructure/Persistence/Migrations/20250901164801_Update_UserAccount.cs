using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_UserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "UserAccount",
                newName: "TelegramAccount");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UserAccount",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "UserAccount",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UserAccount");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "UserAccount");

            migrationBuilder.RenameColumn(
                name: "TelegramAccount",
                table: "UserAccount",
                newName: "FullName");
        }
    }
}
