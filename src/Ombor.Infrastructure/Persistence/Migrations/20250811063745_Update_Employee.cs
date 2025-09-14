using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_Employee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employee");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Employee",
                newName: "Position");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Employee",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfEmployment",
                table: "Employee",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Employee",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Employee",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Employee",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "Employee",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Employee",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DateOfEmployment",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Employee");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "Employee",
                newName: "Role");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employee",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
