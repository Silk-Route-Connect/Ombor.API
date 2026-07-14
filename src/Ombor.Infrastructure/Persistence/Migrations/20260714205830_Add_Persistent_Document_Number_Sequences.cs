using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Persistent_Document_Number_Sequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. The per-organization counter table, created first so the seed step below can populate it.
            migrationBuilder.CreateTable(
                name: "NumberSequence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    SeriesType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequence", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_OrganizationId",
                table: "NumberSequence",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_OrganizationId_SeriesType",
                table: "NumberSequence",
                columns: new[] { "OrganizationId", "SeriesType" },
                unique: true);

            // 2. Add the new nullable Transaction document number.
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "TransactionRecord",
                type: "int",
                nullable: true);

            // 3. Replace Payment.Number and Order.OrderNumber (nvarchar -> int). The old «P-n» strings and GUID
            //    slices can't be converted, so drop and re-add as nullable int; the backfill below assigns fresh
            //    sequential numbers. Payment.Number's filtered unique index must be dropped before its column.
            migrationBuilder.Sql("DROP INDEX [IX_Payment_OrganizationId_Number] ON [Payment];");
            migrationBuilder.DropColumn(name: "Number", table: "Payment");
            migrationBuilder.AddColumn<int>(name: "Number", table: "Payment", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "OrderNumber", table: "Order");
            migrationBuilder.AddColumn<int>(name: "OrderNumber", table: "Order", type: "int", nullable: true);

            // 4. Backfill clean per-organization sequential numbers, ordered by Id. Transactions are ONE series
            //    across all sub-types (partition by OrganizationId only, never by Type).
            migrationBuilder.Sql(@"
                WITH s AS (SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrganizationId] ORDER BY [Id]) AS rn FROM [TransactionRecord])
                UPDATE t SET t.[Number] = s.rn FROM [TransactionRecord] t JOIN s ON s.[Id] = t.[Id];");
            migrationBuilder.Sql(@"
                WITH s AS (SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrganizationId] ORDER BY [Id]) AS rn FROM [Payment])
                UPDATE p SET p.[Number] = s.rn FROM [Payment] p JOIN s ON s.[Id] = p.[Id];");
            migrationBuilder.Sql(@"
                WITH s AS (SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrganizationId] ORDER BY [Id]) AS rn FROM [Order])
                UPDATE o SET o.[OrderNumber] = s.rn FROM [Order] o JOIN s ON s.[Id] = o.[Id];");

            // 5. Seed each counter to the max number used per organization. Organizations with no rows get no
            //    seed row — the allocator lazily creates one starting at 1 on first use.
            migrationBuilder.Sql(@"
                INSERT INTO [NumberSequence] ([OrganizationId], [SeriesType], [LastValue])
                SELECT [OrganizationId], N'Transaction', MAX([Number]) FROM [TransactionRecord] GROUP BY [OrganizationId];");
            migrationBuilder.Sql(@"
                INSERT INTO [NumberSequence] ([OrganizationId], [SeriesType], [LastValue])
                SELECT [OrganizationId], N'Payment', MAX([Number]) FROM [Payment] GROUP BY [OrganizationId];");
            migrationBuilder.Sql(@"
                INSERT INTO [NumberSequence] ([OrganizationId], [SeriesType], [LastValue])
                SELECT [OrganizationId], N'Order', MAX([OrderNumber]) FROM [Order] GROUP BY [OrganizationId];");

            // 6. Filtered unique indexes last, after the backfill guarantees per-organization uniqueness.
            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_OrganizationId_Number",
                table: "TransactionRecord",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrganizationId_Number",
                table: "Payment",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrganizationId_OrderNumber",
                table: "Order",
                columns: new[] { "OrganizationId", "OrderNumber" },
                unique: true,
                filter: "[OrderNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-lossy: the original «P-n» payment numbers and GUID order numbers are not recoverable; the
            // columns revert to their previous shape but not their previous values.
            migrationBuilder.DropIndex(name: "IX_TransactionRecord_OrganizationId_Number", table: "TransactionRecord");
            migrationBuilder.DropIndex(name: "IX_Payment_OrganizationId_Number", table: "Payment");
            migrationBuilder.DropIndex(name: "IX_Order_OrganizationId_OrderNumber", table: "Order");

            migrationBuilder.DropTable(name: "NumberSequence");

            migrationBuilder.DropColumn(name: "Number", table: "TransactionRecord");

            migrationBuilder.DropColumn(name: "Number", table: "Payment");
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Payment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrganizationId_Number",
                table: "Payment",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.DropColumn(name: "OrderNumber", table: "Order");
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Order",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
