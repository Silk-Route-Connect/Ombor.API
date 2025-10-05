using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Update_Employee_ContactInfo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PhoneNumber",
            table: "Employee");

        migrationBuilder.RenameColumn(
            name: "Email",
            table: "Employee",
            newName: "ContactInfo_Email");

        migrationBuilder.RenameColumn(
            name: "Address",
            table: "Employee",
            newName: "ContactInfo_Address");

        migrationBuilder.RenameColumn(
            name: "Description",
            table: "Employee",
            newName: "ContactInfo_PhoneNumbers");

        migrationBuilder.AddColumn<string>(
            name: "ContactInfo_TelegramAccount",
            table: "Employee",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContactInfo_TelegramAccount",
            table: "Employee");

        migrationBuilder.RenameColumn(
            name: "ContactInfo_Email",
            table: "Employee",
            newName: "Email");

        migrationBuilder.RenameColumn(
            name: "ContactInfo_Address",
            table: "Employee",
            newName: "Address");

        migrationBuilder.RenameColumn(
            name: "ContactInfo_PhoneNumbers",
            table: "Employee",
            newName: "Description");

        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "Employee",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: false,
            defaultValue: "");
    }
}
