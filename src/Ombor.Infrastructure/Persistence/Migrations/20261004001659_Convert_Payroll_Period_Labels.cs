using Microsoft.EntityFrameworkCore.Migrations;
using Ombor.Infrastructure.Persistence.DataFixes;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Convert_Payroll_Period_Labels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PayrollPeriodBackfill.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data correction only: the language-specific labels it replaced are not restored.
        }
    }
}
