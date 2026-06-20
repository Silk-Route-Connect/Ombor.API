using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Partner_OpeningBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "Partner",
                newName: "OpeningBalance");

            migrationBuilder.AddColumn<DateOnly>(
                name: "OpeningDate",
                table: "Partner",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // The net balance now starts from the partner's opening balance (M3).
            migrationBuilder.Sql(ViewWithOpeningBalance);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the view to the pre-opening-balance shape before the column is renamed away.
            migrationBuilder.Sql(ViewWithoutOpeningBalance);

            migrationBuilder.DropColumn(
                name: "OpeningDate",
                table: "Partner");

            migrationBuilder.RenameColumn(
                name: "OpeningBalance",
                table: "Partner",
                newName: "Balance");
        }

        private const string ViewWithOpeningBalance = @"
CREATE OR ALTER VIEW dbo.View_PartnerBalance
AS
SELECT
    partner.Id AS PartnerId,
    partner.OrganizationId AS OrganizationId,

    CAST(partner.OpeningBalance AS decimal(18,2)) AS OpeningBalance,

    CAST(ISNULL(PartnerAdvanceParked.Total, 0) - ISNULL(PartnerAdvanceDrawn.Total, 0) AS decimal(18,2)) AS PartnerAdvance,
    CAST(ISNULL(CompanyAdvanceParked.Total, 0) - ISNULL(CompanyAdvanceDrawn.Total, 0) AS decimal(18,2)) AS CompanyAdvance,

    CAST(ISNULL(Debts.PayableDebt,    0) AS decimal(18,2)) AS PayableDebt,
    CAST(ISNULL(Debts.ReceivableDebt, 0) AS decimal(18,2)) AS ReceivableDebt
FROM dbo.Partner AS partner

LEFT JOIN (
    SELECT payment.PartnerId, SUM(allocation.Amount) AS Total
    FROM dbo.PaymentAllocation allocation
    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
    WHERE allocation.Type = N'AdvanceCredit' AND payment.Direction = N'Income'
    GROUP BY payment.PartnerId
) AS PartnerAdvanceParked ON PartnerAdvanceParked.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(component.Amount) AS Total
    FROM dbo.PaymentComponent component
    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
    WHERE component.SourceType = N'Advance' AND payment.Direction = N'Income'
    GROUP BY payment.PartnerId
) AS PartnerAdvanceDrawn ON PartnerAdvanceDrawn.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(allocation.Amount) AS Total
    FROM dbo.PaymentAllocation allocation
    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
    WHERE allocation.Type = N'AdvanceCredit' AND payment.Direction = N'Expense'
    GROUP BY payment.PartnerId
) AS CompanyAdvanceParked ON CompanyAdvanceParked.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(component.Amount) AS Total
    FROM dbo.PaymentComponent component
    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
    WHERE component.SourceType = N'Advance' AND payment.Direction = N'Expense'
    GROUP BY payment.PartnerId
) AS CompanyAdvanceDrawn ON CompanyAdvanceDrawn.PartnerId = partner.Id

LEFT JOIN (
    SELECT PartnerId,
           SUM(CASE WHEN Type IN (N'Supply', N'SaleRefund')
                    THEN TotalDue - TotalPaid END) AS PayableDebt,
           SUM(CASE WHEN Type IN (N'Sale', N'SupplyRefund')
                    THEN TotalDue - TotalPaid END) AS ReceivableDebt
    FROM dbo.TransactionRecord record
    WHERE record.Status != 'Closed'
    GROUP BY record.PartnerId
) AS Debts ON Debts.PartnerId = partner.Id;
";

        private const string ViewWithoutOpeningBalance = @"
CREATE OR ALTER VIEW dbo.View_PartnerBalance
AS
SELECT
    partner.Id AS PartnerId,
    partner.OrganizationId AS OrganizationId,

    CAST(ISNULL(PartnerAdvanceParked.Total, 0) - ISNULL(PartnerAdvanceDrawn.Total, 0) AS decimal(18,2)) AS PartnerAdvance,
    CAST(ISNULL(CompanyAdvanceParked.Total, 0) - ISNULL(CompanyAdvanceDrawn.Total, 0) AS decimal(18,2)) AS CompanyAdvance,

    CAST(ISNULL(Debts.PayableDebt,    0) AS decimal(18,2)) AS PayableDebt,
    CAST(ISNULL(Debts.ReceivableDebt, 0) AS decimal(18,2)) AS ReceivableDebt
FROM dbo.Partner AS partner

LEFT JOIN (
    SELECT payment.PartnerId, SUM(allocation.Amount) AS Total
    FROM dbo.PaymentAllocation allocation
    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
    WHERE allocation.Type = N'AdvanceCredit' AND payment.Direction = N'Income'
    GROUP BY payment.PartnerId
) AS PartnerAdvanceParked ON PartnerAdvanceParked.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(component.Amount) AS Total
    FROM dbo.PaymentComponent component
    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
    WHERE component.SourceType = N'Advance' AND payment.Direction = N'Income'
    GROUP BY payment.PartnerId
) AS PartnerAdvanceDrawn ON PartnerAdvanceDrawn.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(allocation.Amount) AS Total
    FROM dbo.PaymentAllocation allocation
    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
    WHERE allocation.Type = N'AdvanceCredit' AND payment.Direction = N'Expense'
    GROUP BY payment.PartnerId
) AS CompanyAdvanceParked ON CompanyAdvanceParked.PartnerId = partner.Id

LEFT JOIN (
    SELECT payment.PartnerId, SUM(component.Amount) AS Total
    FROM dbo.PaymentComponent component
    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
    WHERE component.SourceType = N'Advance' AND payment.Direction = N'Expense'
    GROUP BY payment.PartnerId
) AS CompanyAdvanceDrawn ON CompanyAdvanceDrawn.PartnerId = partner.Id

LEFT JOIN (
    SELECT PartnerId,
           SUM(CASE WHEN Type IN (N'Supply', N'SaleRefund')
                    THEN TotalDue - TotalPaid END) AS PayableDebt,
           SUM(CASE WHEN Type IN (N'Sale', N'SupplyRefund')
                    THEN TotalDue - TotalPaid END) AS ReceivableDebt
    FROM dbo.TransactionRecord record
    WHERE record.Status != 'Closed'
    GROUP BY record.PartnerId
) AS Debts ON Debts.PartnerId = partner.Id;
";
    }
}
