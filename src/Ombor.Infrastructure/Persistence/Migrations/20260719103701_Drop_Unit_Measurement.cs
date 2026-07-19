using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Drop_Unit_Measurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The UnitOfMeasurement.Unit member was removed (it duplicated Piece). Measurement is persisted
            // as the enum name string, so any existing 'Unit' row would throw on read once the member is gone.
            // Reassign them to the surviving 'Piece' before that can happen. No schema change.
            migrationBuilder.Sql("UPDATE Product SET Measurement = 'Piece' WHERE Measurement = 'Unit';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data migration: once rewritten to 'Piece', the originally-'Unit' rows are
            // indistinguishable from genuine 'Piece' rows, so there is nothing to restore.
        }
    }
}
