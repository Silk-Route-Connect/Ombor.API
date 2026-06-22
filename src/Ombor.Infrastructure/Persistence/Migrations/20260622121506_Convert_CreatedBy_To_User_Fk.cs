using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Convert_CreatedBy_To_User_Fk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WalletTransfer");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Transfer");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockAdjustment");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OpeningStock");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "WalletTransfer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Wallet",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Transfer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "StockAdjustment",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "OpeningStock",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransfer_CreatedById",
                table: "WalletTransfer",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_CreatedById",
                table: "Wallet",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_CreatedById",
                table: "Transfer",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_CreatedById",
                table: "StockAdjustment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OpeningStock_CreatedById",
                table: "OpeningStock",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_OpeningStock_User_CreatedById",
                table: "OpeningStock",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustment_User_CreatedById",
                table: "StockAdjustment",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfer_User_CreatedById",
                table: "Transfer",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallet_User_CreatedById",
                table: "Wallet",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransfer_User_CreatedById",
                table: "WalletTransfer",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpeningStock_User_CreatedById",
                table: "OpeningStock");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustment_User_CreatedById",
                table: "StockAdjustment");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfer_User_CreatedById",
                table: "Transfer");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallet_User_CreatedById",
                table: "Wallet");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransfer_User_CreatedById",
                table: "WalletTransfer");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransfer_CreatedById",
                table: "WalletTransfer");

            migrationBuilder.DropIndex(
                name: "IX_Wallet_CreatedById",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_Transfer_CreatedById",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustment_CreatedById",
                table: "StockAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_OpeningStock_CreatedById",
                table: "OpeningStock");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "WalletTransfer");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Transfer");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "StockAdjustment");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "OpeningStock");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "WalletTransfer",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Wallet",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Transfer",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StockAdjustment",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OpeningStock",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
