using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Ombor.Infrastructure.Persistence.DataFixes;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Audit_Operations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_EntityType_EntityId",
                table: "AuditEntry");

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                table: "AuditEntry",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentEntityId",
                table: "AuditEntry",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentEntityType",
                table: "AuditEntry",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql(AuditOperationBackfill.Sql);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_OrganizationId_EntityType_EntityId",
                table: "AuditEntry",
                columns: new[] { "OrganizationId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_OrganizationId_OperationId",
                table: "AuditEntry",
                columns: new[] { "OrganizationId", "OperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_OrganizationId_ParentEntityType_ParentEntityId",
                table: "AuditEntry",
                columns: new[] { "OrganizationId", "ParentEntityType", "ParentEntityId" },
                filter: "[ParentEntityType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_OrganizationId_TimestampUtc",
                table: "AuditEntry",
                columns: new[] { "OrganizationId", "TimestampUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_OrganizationId_UserId_TimestampUtc",
                table: "AuditEntry",
                columns: new[] { "OrganizationId", "UserId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_OrganizationId_EntityType_EntityId",
                table: "AuditEntry");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_OrganizationId_OperationId",
                table: "AuditEntry");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_OrganizationId_ParentEntityType_ParentEntityId",
                table: "AuditEntry");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_OrganizationId_TimestampUtc",
                table: "AuditEntry");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntry_OrganizationId_UserId_TimestampUtc",
                table: "AuditEntry");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "AuditEntry");

            migrationBuilder.DropColumn(
                name: "ParentEntityId",
                table: "AuditEntry");

            migrationBuilder.DropColumn(
                name: "ParentEntityType",
                table: "AuditEntry");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_EntityType_EntityId",
                table: "AuditEntry",
                columns: new[] { "EntityType", "EntityId" });
        }
    }
}
