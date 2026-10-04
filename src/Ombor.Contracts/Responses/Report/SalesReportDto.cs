using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// Sales and sale refunds in a period, with their cost and gross profit. Every amount comes from the document lines
/// (net of line discounts), so the totals are the same whatever the grouping and reconcile with the Sales list.
/// </summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="GroupBy">How <paramref name="Rows"/> are split.</param>
/// <param name="Rows">One row per bucket (every day/week/month of the period) or per product/category/partner/warehouse with sales or refunds (largest net revenue first).</param>
/// <param name="Totals">The whole period.</param>
/// <param name="CostIsEstimated">True when any cost in the period is an estimate (lines recorded before cost snapshots began on 2026-10-04) — show profit with a warning.</param>
public sealed record SalesReportDto(
    DateOnly From,
    DateOnly To,
    ReportGroupBy GroupBy,
    SalesReportRowDto[] Rows,
    SalesReportTotalsDto Totals,
    bool CostIsEstimated);

/// <summary>One row of the sales report.</summary>
/// <param name="Key">Stable key: a date (<c>yyyy-MM-dd</c>; a week's Monday) or a month (<c>yyyy-MM</c>) for time groupings, the entity id otherwise.</param>
/// <param name="Label">What to show: the same date/month for time groupings, the product/category/partner/warehouse name otherwise.</param>
/// <param name="Documents">Sale documents in the row.</param>
/// <param name="RefundDocuments">Sale-refund documents in the row.</param>
/// <param name="Quantity">Units sold minus units returned, in base units (rows spanning several products add different units).</param>
/// <param name="Revenue">Sales, net of line discounts.</param>
/// <param name="Refunds">Sale refunds.</param>
/// <param name="NetRevenue"><paramref name="Revenue"/> − <paramref name="Refunds"/>.</param>
/// <param name="Cost">Cost of goods sold minus the cost of goods returned (the snapshotted line costs).</param>
/// <param name="GrossProfit"><paramref name="NetRevenue"/> − <paramref name="Cost"/>.</param>
/// <param name="MarginPercent"><paramref name="GrossProfit"/> ÷ <paramref name="NetRevenue"/> × 100, 2 decimals; null unless net revenue is positive.</param>
/// <param name="CostIsEstimated">True when any cost in the row is an estimate.</param>
public sealed record SalesReportRowDto(
    string Key,
    string Label,
    int Documents,
    int RefundDocuments,
    decimal Quantity,
    decimal Revenue,
    decimal Refunds,
    decimal NetRevenue,
    decimal Cost,
    decimal GrossProfit,
    decimal? MarginPercent,
    bool CostIsEstimated);

/// <summary>The sales report's figures for the whole period (fields as on <see cref="SalesReportRowDto"/>).</summary>
/// <param name="Documents">Sale documents.</param>
/// <param name="RefundDocuments">Sale-refund documents.</param>
/// <param name="Quantity">Units sold minus units returned (base units of different products added together).</param>
/// <param name="Revenue">Sales, net of line discounts.</param>
/// <param name="Refunds">Sale refunds.</param>
/// <param name="NetRevenue">Revenue − refunds.</param>
/// <param name="Cost">Cost of goods sold net of returns.</param>
/// <param name="GrossProfit">Net revenue − cost.</param>
/// <param name="MarginPercent">Gross profit ÷ net revenue × 100, 2 decimals; null unless net revenue is positive.</param>
public sealed record SalesReportTotalsDto(
    int Documents,
    int RefundDocuments,
    decimal Quantity,
    decimal Revenue,
    decimal Refunds,
    decimal NetRevenue,
    decimal Cost,
    decimal GrossProfit,
    decimal? MarginPercent);
