using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Employee_Payment_Relationship : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "PartnerId",
            table: "Payment",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AddColumn<int>(
            name: "EmployeeId",
            table: "Payment",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payment_EmployeeId",
            table: "Payment",
            column: "EmployeeId");

        migrationBuilder.AddForeignKey(
            name: "FK_Payment_Employee_EmployeeId",
            table: "Payment",
            column: "EmployeeId",
            principalTable: "Employee",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Payment_Employee_EmployeeId",
            table: "Payment");

        migrationBuilder.DropIndex(
            name: "IX_Payment_EmployeeId",
            table: "Payment");

        migrationBuilder.DropColumn(
            name: "EmployeeId",
            table: "Payment");

        migrationBuilder.AlterColumn<int>(
            name: "PartnerId",
            table: "Payment",
            type: "int",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }
}
