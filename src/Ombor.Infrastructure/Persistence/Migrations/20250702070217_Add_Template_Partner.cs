using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Template_Partner : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PartnerId",
            table: "Template",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_Template_PartnerId",
            table: "Template",
            column: "PartnerId");

        migrationBuilder.AddForeignKey(
            name: "FK_Template_Partner_PartnerId",
            table: "Template",
            column: "PartnerId",
            principalTable: "Partner",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Template_Partner_PartnerId",
            table: "Template");

        migrationBuilder.DropIndex(
            name: "IX_Template_PartnerId",
            table: "Template");

        migrationBuilder.DropColumn(
            name: "PartnerId",
            table: "Template");
    }
}
