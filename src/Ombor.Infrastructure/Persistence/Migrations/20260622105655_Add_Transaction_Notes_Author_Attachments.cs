using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Transaction_Notes_Author_Attachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "TransactionRecord",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TransactionRecord",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionAttachment_TransactionRecord_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TransactionRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_CreatedById",
                table: "TransactionRecord",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAttachment_OrganizationId",
                table: "TransactionAttachment",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAttachment_TransactionId",
                table: "TransactionAttachment",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionRecord_User_CreatedById",
                table: "TransactionRecord",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionRecord_User_CreatedById",
                table: "TransactionRecord");

            migrationBuilder.DropTable(
                name: "TransactionAttachment");

            migrationBuilder.DropIndex(
                name: "IX_TransactionRecord_CreatedById",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "TransactionRecord");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TransactionRecord");
        }
    }
}
