using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rewrite_PartnerBalance_View_For_Payment_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Derive advances from the source/allocation payment model: a partner's advance is the
            // money parked via AdvanceCredit minus the money later drawn via an Advance-source
            // component, on payments in the matching direction (Income for the partner, Expense for us).
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dbo.View_PartnerBalance
                AS
                SELECT
                    partner.Id AS PartnerId,
                    partner.OrganizationId AS OrganizationId,

                    CAST(
                        ISNULL(PartnerAdvanceMade.TotalAdvance, 0) -
                        ISNULL(PartnerAdvanceUsed.TotalUsed, 0)
                        AS decimal(18,2)
                    ) AS PartnerAdvance,

                    CAST(
                        ISNULL(CompanyAdvanceMade.TotalAdvance, 0) -
                        ISNULL(CompanyAdvanceUsed.TotalUsed,   0)
                        AS decimal(18,2)
                    ) AS CompanyAdvance,

                    CAST(ISNULL(UnpaidTotals.PayableDebt,    0) AS decimal(18,2)) AS PayableDebt,
                    CAST(ISNULL(UnpaidTotals.ReceivableDebt, 0) AS decimal(18,2)) AS ReceivableDebt
                FROM dbo.Partner AS partner

                LEFT JOIN (
                    SELECT payment.PartnerId, SUM(allocation.Amount) AS TotalAdvance
                    FROM dbo.PaymentAllocation allocation
                    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
                    WHERE allocation.Type = N'AdvancePayment' AND payment.Direction = N'Income'
                    GROUP BY payment.PartnerId
                ) AS PartnerAdvanceMade ON PartnerAdvanceMade.PartnerId = partner.Id

                LEFT JOIN (
                    SELECT payment.PartnerId, SUM(ROUND(component.Amount * component.ExchangeRate, 2)) AS TotalUsed
                    FROM dbo.PaymentComponent component
                    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
                    WHERE component.Method = N'AccountBalance' AND payment.Direction = N'Income'
                    GROUP BY payment.PartnerId
                ) AS PartnerAdvanceUsed ON PartnerAdvanceUsed.PartnerId = partner.Id

                LEFT JOIN (
                    SELECT payment.PartnerId, SUM(allocation.Amount) AS TotalAdvance
                    FROM dbo.PaymentAllocation allocation
                    JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
                    WHERE allocation.Type = N'AdvancePayment' AND  payment.Direction = N'Expense'
                    GROUP BY payment.PartnerId
                ) AS CompanyAdvanceMade ON CompanyAdvanceMade.PartnerId = partner.Id

                LEFT JOIN (
                    SELECT payment.PartnerId, SUM(ROUND(component.Amount * component.ExchangeRate, 2)) AS TotalUsed
                    FROM dbo.PaymentComponent component
                    JOIN dbo.Payment payment ON payment.Id = component.PaymentId
                    WHERE component.Method    = N'AccountBalance' AND  payment.Direction = N'Expense'
                    GROUP BY payment.PartnerId
                ) AS CompanyAdvanceUsed ON CompanyAdvanceUsed.PartnerId = partner.Id

                LEFT JOIN (
                    SELECT PartnerId,
                           SUM(CASE WHEN Type IN (N'Sale', N'SupplyRefund')
                                    THEN TotalDue - TotalPaid END) AS PayableDebt,
                           SUM(CASE WHEN Type IN (N'Supply', N'SaleRefund')
                                    THEN TotalDue - TotalPaid END) AS ReceivableDebt
                    FROM dbo.TransactionRecord record
                    WHERE record.Status != 'Closed'
                    GROUP BY record.PartnerId
                ) AS UnpaidTotals ON UnpaidTotals.PartnerId = partner.Id;
            ");
        }
    }
}
