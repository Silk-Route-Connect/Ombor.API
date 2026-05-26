using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Multi_Tenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Partner_CustomerId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Organization_OrganizationId",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Organization_OrganizationId",
                table: "User");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "User",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_User_OrganizationId",
                table: "User",
                newName: "IX_User_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Role",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Role_OrganizationId",
                table: "Role",
                newName: "IX_Role_TenantId");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TransactionRecord",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TransactionLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TemplateItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ProductImage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PaymentComponent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PaymentAttachment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PaymentAllocation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Partner",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "InventoryItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Inventory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Employee",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Category",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_TenantId",
                table: "TransactionRecord",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLine_TenantId",
                table: "TransactionLine",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItem_TenantId",
                table: "TemplateItem",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Template_TenantId",
                table: "Template",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_TenantId",
                table: "ProductImage",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_TenantId",
                table: "Product",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentComponent_TenantId",
                table: "PaymentComponent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttachment_TenantId",
                table: "PaymentAttachment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId",
                table: "PaymentAllocation",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TenantId",
                table: "Payment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Partner_TenantId",
                table: "Partner",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLine_TenantId",
                table: "OrderLine",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_TenantId",
                table: "Order",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItem_TenantId",
                table: "InventoryItem",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_TenantId",
                table: "Inventory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_TenantId",
                table: "Employee",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_TenantId",
                table: "Category",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Partner_CustomerId",
                table: "Order",
                column: "CustomerId",
                principalTable: "Partner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Tenant_TenantId",
                table: "Role",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Tenant_TenantId",
                table: "User",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Partner_CustomerId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Tenant_TenantId",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Tenant_TenantId",
                table: "User");

            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_TransactionRecord_TenantId",
                table: "TransactionRecord");

            migrationBuilder.DropIndex(
                name: "IX_TransactionLine_TenantId",
                table: "TransactionLine");

            migrationBuilder.DropIndex(
                name: "IX_TemplateItem_TenantId",
                table: "TemplateItem");

            migrationBuilder.DropIndex(
                name: "IX_Template_TenantId",
                table: "Template");

            migrationBuilder.DropIndex(
                name: "IX_ProductImage_TenantId",
                table: "ProductImage");

            migrationBuilder.DropIndex(
                name: "IX_Product_TenantId",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_PaymentComponent_TenantId",
                table: "PaymentComponent");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttachment_TenantId",
                table: "PaymentAttachment");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocation_TenantId",
                table: "PaymentAllocation");

            migrationBuilder.DropIndex(
                name: "IX_Payment_TenantId",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Partner_TenantId",
                table: "Partner");

            migrationBuilder.DropIndex(
                name: "IX_OrderLine_TenantId",
                table: "OrderLine");

            migrationBuilder.DropIndex(
                name: "IX_Order_TenantId",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItem_TenantId",
                table: "InventoryItem");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_TenantId",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Employee_TenantId",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Category_TenantId",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TransactionLine");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TemplateItem");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductImage");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentComponent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentAttachment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentAllocation");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OrderLine");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InventoryItem");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Category");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "User",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_User_TenantId",
                table: "User",
                newName: "IX_User_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Role",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Role_TenantId",
                table: "Role",
                newName: "IX_Role_OrganizationId");

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Partner_CustomerId",
                table: "Order",
                column: "CustomerId",
                principalTable: "Partner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Organization_OrganizationId",
                table: "Role",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Organization_OrganizationId",
                table: "User",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
