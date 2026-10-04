using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// A simple profit and loss for a period: net revenue − cost of goods sold = gross profit; − stock losses − payroll −
/// other expenses = profit. Each figure equals the matching report for the same days (sales, losses, expenses).
/// </summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="GroupBy">Day, Week or Month.</param>
/// <param name="Rows">Every bucket of the period, oldest first.</param>
/// <param name="Totals">The whole period.</param>
/// <param name="CostIsEstimated">True when any cost of goods sold in the period is an estimate — show profit with a warning.</param>
public sealed record ProfitReportDto(
    DateOnly From,
    DateOnly To,
    ReportGroupBy GroupBy,
    ProfitReportRowDto[] Rows,
    ProfitReportTotalsDto Totals,
    bool CostIsEstimated);

/// <summary>One bucket of the profit report.</summary>
/// <param name="Key">A date (<c>yyyy-MM-dd</c>; a week's Monday) or a month (<c>yyyy-MM</c>).</param>
/// <param name="Label">The same date or month.</param>
/// <param name="Revenue">Sales, net of line discounts.</param>
/// <param name="Refunds">Sale refunds.</param>
/// <param name="NetRevenue"><paramref name="Revenue"/> − <paramref name="Refunds"/>.</param>
/// <param name="Cost">Cost of goods sold net of returns.</param>
/// <param name="GrossProfit"><paramref name="NetRevenue"/> − <paramref name="Cost"/>.</param>
/// <param name="Losses">Stock written off by decrease adjustments, at the cost snapshotted on each.</param>
/// <param name="Payroll">Payroll paid out of wallets.</param>
/// <param name="OtherExpenses">Other (General) expense payments paid out of wallets.</param>
/// <param name="Profit"><paramref name="GrossProfit"/> − <paramref name="Losses"/> − <paramref name="Payroll"/> − <paramref name="OtherExpenses"/>.</param>
/// <param name="CostIsEstimated">True when any cost of goods sold in the bucket is an estimate.</param>
public sealed record ProfitReportRowDto(
    string Key,
    string Label,
    decimal Revenue,
    decimal Refunds,
    decimal NetRevenue,
    decimal Cost,
    decimal GrossProfit,
    decimal Losses,
    decimal Payroll,
    decimal OtherExpenses,
    decimal Profit,
    bool CostIsEstimated);

/// <summary>The profit report's figures for the whole period (fields as on <see cref="ProfitReportRowDto"/>).</summary>
/// <param name="Revenue">Sales, net of line discounts.</param>
/// <param name="Refunds">Sale refunds.</param>
/// <param name="NetRevenue">Revenue − refunds.</param>
/// <param name="Cost">Cost of goods sold net of returns.</param>
/// <param name="GrossProfit">Net revenue − cost.</param>
/// <param name="Losses">Stock written off.</param>
/// <param name="Payroll">Payroll paid.</param>
/// <param name="OtherExpenses">Other (General) expenses paid.</param>
/// <param name="Profit">Gross profit − losses − payroll − other expenses.</param>
public sealed record ProfitReportTotalsDto(
    decimal Revenue,
    decimal Refunds,
    decimal NetRevenue,
    decimal Cost,
    decimal GrossProfit,
    decimal Losses,
    decimal Payroll,
    decimal OtherExpenses,
    decimal Profit);
