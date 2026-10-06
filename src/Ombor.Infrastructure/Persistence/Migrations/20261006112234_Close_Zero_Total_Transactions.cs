using Microsoft.EntityFrameworkCore.Migrations;
using Ombor.Infrastructure.Persistence.DataFixes;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Re-runs the settlement-status backfill now that a document with nothing due counts as Closed: databases that
    /// already applied <c>Recompute_Transaction_Settlement_Status</c> with the earlier rule still hold free or fully
    /// discounted documents as Open. Idempotent — a database migrated from scratch has nothing left to change.
    /// </summary>
    public partial class Close_Zero_Total_Transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(TransactionStatusBackfill.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data correction only: the wrong statuses it replaced are not worth restoring.
        }
    }
}
