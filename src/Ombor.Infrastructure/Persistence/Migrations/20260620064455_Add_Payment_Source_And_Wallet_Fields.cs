using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Payment_Source_And_Wallet_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "PaymentComponent",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Wallet");

            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "PaymentComponent",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Payment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "Payment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentComponent_WalletId",
                table: "PaymentComponent",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_WalletId",
                table: "Payment",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Wallet_WalletId",
                table: "Payment",
                column: "WalletId",
                principalTable: "Wallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentComponent_Wallet_WalletId",
                table: "PaymentComponent",
                column: "WalletId",
                principalTable: "Wallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Wallet_WalletId",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentComponent_Wallet_WalletId",
                table: "PaymentComponent");

            migrationBuilder.DropIndex(
                name: "IX_PaymentComponent_WalletId",
                table: "PaymentComponent");

            migrationBuilder.DropIndex(
                name: "IX_Payment_WalletId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "PaymentComponent");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "PaymentComponent");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Payment");
        }
    }
}
