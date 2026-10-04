using Microsoft.EntityFrameworkCore.Migrations;
using Ombor.Infrastructure.Persistence.DataFixes;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Recompute_Transaction_Settlement_Status : Migration
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
