using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rename_Tenant_To_Organization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Non-destructive rename: keep the table and its rows, just rename the table,
            // its primary key, and the two foreign keys that point at it. (EF scaffolds a
            // drop/create here because it cannot detect renames; that would lose every
            // organization, so it is replaced with sp_rename-backed rename operations.)
            migrationBuilder.RenameTable(
                name: "Tenant",
                newName: "Organization");

            migrationBuilder.Sql("EXEC sp_rename N'PK_Tenant', N'PK_Organization', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Role_Tenant_TenantId', N'FK_Role_Organization_OrganizationId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_User_Tenant_TenantId', N'FK_User_Organization_OrganizationId', N'OBJECT';");

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
                table: "TransferLine",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferLine_TenantId",
                table: "TransferLine",
                newName: "IX_TransferLine_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Transfer",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfer_TenantId",
                table: "Transfer",
                newName: "IX_Transfer_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "TransactionRecord",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionRecord_TenantId",
                table: "TransactionRecord",
                newName: "IX_TransactionRecord_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "TransactionLine",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionLine_TenantId",
                table: "TransactionLine",
                newName: "IX_TransactionLine_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "TemplateItem",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateItem_TenantId",
                table: "TemplateItem",
                newName: "IX_TemplateItem_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Template",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Template_TenantId",
                table: "Template",
                newName: "IX_Template_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Role",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Role_TenantId",
                table: "Role",
                newName: "IX_Role_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "ProductImage",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductImage_TenantId",
                table: "ProductImage",
                newName: "IX_ProductImage_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Product",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Product_TenantId_SKU",
                table: "Product",
                newName: "IX_Product_OrganizationId_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Product_TenantId",
                table: "Product",
                newName: "IX_Product_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "PaymentComponent",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentComponent_TenantId",
                table: "PaymentComponent",
                newName: "IX_PaymentComponent_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "PaymentAttachment",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAttachment_TenantId",
                table: "PaymentAttachment",
                newName: "IX_PaymentAttachment_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "PaymentAllocation",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAllocation_TenantId",
                table: "PaymentAllocation",
                newName: "IX_PaymentAllocation_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Payment",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Payment_TenantId",
                table: "Payment",
                newName: "IX_Payment_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Partner",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Partner_TenantId",
                table: "Partner",
                newName: "IX_Partner_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "OrderLine",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderLine_TenantId",
                table: "OrderLine",
                newName: "IX_OrderLine_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Order",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Order_TenantId",
                table: "Order",
                newName: "IX_Order_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "InventoryItem",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItem_TenantId",
                table: "InventoryItem",
                newName: "IX_InventoryItem_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Inventory",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Inventory_TenantId",
                table: "Inventory",
                newName: "IX_Inventory_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Employee",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Employee_TenantId",
                table: "Employee",
                newName: "IX_Employee_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Category",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Category_TenantId",
                table: "Category",
                newName: "IX_Category_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "AuditEntry",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditEntry_TenantId",
                table: "AuditEntry",
                newName: "IX_AuditEntry_OrganizationId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Organization",
                newName: "Tenant");

            migrationBuilder.Sql("EXEC sp_rename N'PK_Organization', N'PK_Tenant', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Role_Organization_OrganizationId', N'FK_Role_Tenant_TenantId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_User_Organization_OrganizationId', N'FK_User_Tenant_TenantId', N'OBJECT';");

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
                table: "TransferLine",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_TransferLine_OrganizationId",
                table: "TransferLine",
                newName: "IX_TransferLine_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Transfer",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfer_OrganizationId",
                table: "Transfer",
                newName: "IX_Transfer_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "TransactionRecord",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionRecord_OrganizationId",
                table: "TransactionRecord",
                newName: "IX_TransactionRecord_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "TransactionLine",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionLine_OrganizationId",
                table: "TransactionLine",
                newName: "IX_TransactionLine_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "TemplateItem",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateItem_OrganizationId",
                table: "TemplateItem",
                newName: "IX_TemplateItem_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Template",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Template_OrganizationId",
                table: "Template",
                newName: "IX_Template_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Role",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Role_OrganizationId",
                table: "Role",
                newName: "IX_Role_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "ProductImage",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductImage_OrganizationId",
                table: "ProductImage",
                newName: "IX_ProductImage_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Product",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Product_OrganizationId_SKU",
                table: "Product",
                newName: "IX_Product_TenantId_SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Product_OrganizationId",
                table: "Product",
                newName: "IX_Product_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "PaymentComponent",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentComponent_OrganizationId",
                table: "PaymentComponent",
                newName: "IX_PaymentComponent_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "PaymentAttachment",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAttachment_OrganizationId",
                table: "PaymentAttachment",
                newName: "IX_PaymentAttachment_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "PaymentAllocation",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAllocation_OrganizationId",
                table: "PaymentAllocation",
                newName: "IX_PaymentAllocation_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Payment",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Payment_OrganizationId",
                table: "Payment",
                newName: "IX_Payment_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Partner",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Partner_OrganizationId",
                table: "Partner",
                newName: "IX_Partner_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "OrderLine",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderLine_OrganizationId",
                table: "OrderLine",
                newName: "IX_OrderLine_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Order",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Order_OrganizationId",
                table: "Order",
                newName: "IX_Order_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "InventoryItem",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItem_OrganizationId",
                table: "InventoryItem",
                newName: "IX_InventoryItem_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Inventory",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Inventory_OrganizationId",
                table: "Inventory",
                newName: "IX_Inventory_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Employee",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Employee_OrganizationId",
                table: "Employee",
                newName: "IX_Employee_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Category",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Category_OrganizationId",
                table: "Category",
                newName: "IX_Category_TenantId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "AuditEntry",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditEntry_OrganizationId",
                table: "AuditEntry",
                newName: "IX_AuditEntry_TenantId");

        }
    }
}
