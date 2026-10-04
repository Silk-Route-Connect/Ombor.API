namespace Ombor.Contracts.Requests.Report;

/// <summary>The cash-flow report: money in and out of wallets in a period.</summary>
/// <param name="From">First local (Tashkent) calendar day, inclusive; default the first day of <paramref name="To"/>'s month.</param>
/// <param name="To">Last local calendar day, inclusive; default today.</param>
/// <param name="WalletId">Only this wallet; default every wallet, archived ones included (rule 31).</param>
public sealed record GetCashFlowReportRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    int? WalletId = null);
