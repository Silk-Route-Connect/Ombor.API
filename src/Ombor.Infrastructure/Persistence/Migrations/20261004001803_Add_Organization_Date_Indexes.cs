using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Organization_Date_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_OrganizationId_DateUtc",
                table: "TransactionRecord",
                columns: new[] { "OrganizationId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_OrganizationId_DateUtc",
                table: "StockAdjustment",
                columns: new[] { "OrganizationId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrganizationId_DateUtc",
                table: "Payment",
                columns: new[] { "OrganizationId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrganizationId_DateUtc",
                table: "Order",
                columns: new[] { "OrganizationId", "DateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionRecord_OrganizationId_DateUtc",
                table: "TransactionRecord");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustment_OrganizationId_DateUtc",
                table: "StockAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_OrganizationId_DateUtc",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Order_OrganizationId_DateUtc",
                table: "Order");
        }
    }
}
