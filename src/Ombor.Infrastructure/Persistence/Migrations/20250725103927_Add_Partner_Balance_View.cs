using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ombor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Partner_Balance_View : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF OBJECT_ID('dbo.View_PartnerBalance', 'V') IS NOT NULL
            DROP VIEW dbo.View_PartnerBalance;
        ");

        migrationBuilder.Sql(@"
            /* Partner-level balance view */
            CREATE OR ALTER VIEW dbo.View_PartnerBalance
            AS
            SELECT
                partner.Id AS PartnerId,

                /* Advance balance of the partner */
                CAST(
		            ISNULL(PartnerAdvanceMade.TotalAdvance, 0) -
		            ISNULL(PartnerAdvanceUsed.TotalUsed, 0) 
		            AS decimal(18,2)
	            ) AS PartnerAdvance,

                /* Advance balance of the company */
                CAST(
                    ISNULL(CompanyAdvanceMade.TotalAdvance, 0) -
		            ISNULL(CompanyAdvanceUsed.TotalUsed,   0)
		            AS decimal(18,2)
                ) AS CompanyAdvance,

                /* Unpaid Sale + SupplyRefund */
                CAST(ISNULL(UnpaidTotals.PayableDebt,    0) AS decimal(18,2)) AS PayableDebt,

                /* Unpaid Supply + SaleRefund */
                CAST(ISNULL(UnpaidTotals.ReceivableDebt, 0) AS decimal(18,2)) AS ReceivableDebt
            FROM dbo.Partner AS partner

            /* Advances paid by partner (Direction = Income) */
            LEFT JOIN (
                SELECT payment.PartnerId, SUM(allocation.Amount) AS TotalAdvance
                FROM dbo.PaymentAllocation allocation
                JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
                WHERE allocation.Type = N'AdvancePayment' AND payment.Direction = N'Income'
                GROUP BY payment.PartnerId
            ) AS PartnerAdvanceMade ON PartnerAdvanceMade.PartnerId = partner.Id

            /* Prior advance USED by partner via AccountBalance (Direction = Income) */
            LEFT JOIN (
                SELECT payment.PartnerId, SUM(ROUND(component.Amount * component.ExchangeRate, 2)) AS TotalUsed
                FROM dbo.PaymentComponent component
                JOIN dbo.Payment payment ON payment.Id = component.PaymentId
                WHERE component.Method = N'AccountBalance' AND payment.Direction = N'Income'
                GROUP BY payment.PartnerId
            ) AS PartnerAdvanceUsed ON PartnerAdvanceUsed.PartnerId = partner.Id

            /* Advances paid by company to partner (Direction = Expense) */
            LEFT JOIN (
                SELECT payment.PartnerId, SUM(allocation.Amount) AS TotalAdvance
                FROM dbo.PaymentAllocation allocation
                JOIN dbo.Payment payment ON payment.Id = allocation.PaymentId
                WHERE allocation.Type = N'AdvancePayment' AND  payment.Direction = N'Expense'
                GROUP BY payment.PartnerId
            ) AS CompanyAdvanceMade ON CompanyAdvanceMade.PartnerId = partner.Id

            /* Prior advance USED by company via AccountBalance (Direction = Expense) */
            LEFT JOIN (
                SELECT payment.PartnerId, SUM(ROUND(component.Amount * component.ExchangeRate, 2)) AS TotalUsed
                FROM dbo.PaymentComponent component
                JOIN dbo.Payment payment ON payment.Id = component.PaymentId
                WHERE component.Method    = N'AccountBalance' AND  payment.Direction = N'Expense'
                GROUP BY payment.PartnerId
            ) AS CompanyAdvanceUsed ON CompanyAdvanceUsed.PartnerId = partner.Id

            /* Unpaid transaction totals */
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
            GO
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF OBJECT_ID('dbo.View_PartnerBalance', 'V') IS NOT NULL
            DROP VIEW dbo.View_PartnerBalance;
        ");
    }
}
