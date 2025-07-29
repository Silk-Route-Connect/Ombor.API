using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Payment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Payment",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Direction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PartnerId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payment_Partner_PartnerId",
                    column: x => x.PartnerId,
                    principalTable: "Partner",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentAllocation",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                TransactionId = table.Column<int>(type: "int", nullable: true),
                PaymentId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAllocation", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentAllocation_Payment_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PaymentAllocation_TransactionRecord_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "TransactionRecord",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "PaymentAttachment",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FileId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                PaymentId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAttachment", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentAttachment_Payment_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PaymentComponent",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PaymentId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentComponent", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentComponent_Payment_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payment_PartnerId",
            table: "Payment",
            column: "PartnerId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAllocation_PaymentId",
            table: "PaymentAllocation",
            column: "PaymentId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAllocation_TransactionId",
            table: "PaymentAllocation",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttachment_PaymentId",
            table: "PaymentAttachment",
            column: "PaymentId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentComponent_PaymentId",
            table: "PaymentComponent",
            column: "PaymentId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PaymentAllocation");

        migrationBuilder.DropTable(
            name: "PaymentAttachment");

        migrationBuilder.DropTable(
            name: "PaymentComponent");

        migrationBuilder.DropTable(
            name: "Payment");
    }
}
