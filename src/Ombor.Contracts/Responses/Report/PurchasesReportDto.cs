using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Report;

/// <summary>Supplies and supply refunds in a period, from the document lines (net of line discounts).</summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="GroupBy">How <paramref name="Rows"/> are split.</param>
/// <param name="Rows">One row per bucket (every day/week/month of the period) or per product/category/partner/warehouse with supplies or returns (largest net purchases first).</param>
/// <param name="Totals">The whole period.</param>
public sealed record PurchasesReportDto(
    DateOnly From,
    DateOnly To,
    ReportGroupBy GroupBy,
    PurchasesReportRowDto[] Rows,
    PurchasesReportTotalsDto Totals);

/// <summary>One row of the purchases report.</summary>
/// <param name="Key">Stable key: a date (<c>yyyy-MM-dd</c>; a week's Monday) or a month (<c>yyyy-MM</c>) for time groupings, the entity id otherwise.</param>
/// <param name="Label">What to show: the same date/month for time groupings, the name otherwise.</param>
/// <param name="Documents">Supply documents in the row.</param>
/// <param name="RefundDocuments">Supply-refund documents in the row.</param>
/// <param name="Quantity">Units supplied minus units returned to suppliers, in base units.</param>
/// <param name="Purchases">Supplies, net of line discounts.</param>
/// <param name="Refunds">Supply refunds (goods returned to suppliers).</param>
/// <param name="NetPurchases"><paramref name="Purchases"/> − <paramref name="Refunds"/>.</param>
public sealed record PurchasesReportRowDto(
    string Key,
    string Label,
    int Documents,
    int RefundDocuments,
    decimal Quantity,
    decimal Purchases,
    decimal Refunds,
    decimal NetPurchases);

/// <summary>The purchases report's figures for the whole period.</summary>
/// <param name="Documents">Supply documents.</param>
/// <param name="RefundDocuments">Supply-refund documents.</param>
/// <param name="Quantity">Units supplied minus units returned (base units of different products added together).</param>
/// <param name="Purchases">Supplies, net of line discounts.</param>
/// <param name="Refunds">Supply refunds.</param>
/// <param name="NetPurchases">Purchases − refunds.</param>
public sealed record PurchasesReportTotalsDto(
    int Documents,
    int RefundDocuments,
    decimal Quantity,
    decimal Purchases,
    decimal Refunds,
    decimal NetPurchases);
