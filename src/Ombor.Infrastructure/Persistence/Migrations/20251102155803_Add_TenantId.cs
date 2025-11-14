using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_TenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "TransactionRecord",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "TransactionLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "TemplateItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "RefreshToken",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "ProductImage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Permission",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "PaymentComponent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "PaymentAttachment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "PaymentAllocation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Partner",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Organization",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "InventoryItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Inventory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Employee",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Category",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "TransactionLine");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "TemplateItem");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ProductImage");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PaymentComponent");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PaymentAttachment");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PaymentAllocation");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "InventoryItem");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Category");
        }
    }
}
