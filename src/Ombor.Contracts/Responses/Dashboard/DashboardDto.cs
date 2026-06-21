namespace Ombor.Contracts.Responses.Dashboard;

/// <summary>
/// The dashboard read model — an aggregated snapshot for the current organization. Debt figures
/// (receivable/payable/overdue/aging/top-debtors) are a current snapshot and reconcile with
/// <c>GET /api/debts</c>; only <see cref="Revenue"/> and <see cref="Series"/> respect the period.
/// </summary>
/// <param name="BusinessName">The organization's name.</param>
/// <param name="Period">The requested period ("today" / "week" / "month").</param>
/// <param name="Revenue">Gross sales in the period (period-driven).</param>
/// <param name="Receivable">Money partners owe us (snapshot).</param>
/// <param name="Payable">Money we owe partners (snapshot).</param>
/// <param name="Overdue">Receivables aged 31+ days — the «Просрочено» definition, distinct from due-date overdue (§K).</param>
/// <param name="Series">Per-bucket sales/supplies/pay-in/pay-out time series for the period.</param>
/// <param name="Wallets">Wallets, for the payments chart's per-wallet filter (aligned to the series wallet arrays).</param>
/// <param name="Aging">Receivable amounts bucketed by age.</param>
/// <param name="TopDebtors">Partners with the largest receivable.</param>
/// <param name="RecentTransactions">The most recent sales and supplies.</param>
public sealed record DashboardDto(
    string BusinessName,
    string Period,
    DashboardKpiDto Revenue,
    DashboardKpiDto Receivable,
    DashboardKpiDto Payable,
    DashboardOverdueKpiDto Overdue,
    DashboardSeriesPointDto[] Series,
    DashboardWalletDto[] Wallets,
    DashboardAgingBucketDto[] Aging,
    DashboardDebtorDto[] TopDebtors,
    DashboardRecentTransactionDto[] RecentTransactions);

/// <summary>A headline figure with its change vs the preceding equal period and a per-bucket trend.</summary>
/// <param name="Value">The headline amount.</param>
/// <param name="DeltaPct">Percent change vs the preceding equal period; null when there's no basis (snapshot KPIs, or a zero prior).</param>
/// <param name="Count">The number of items behind the value.</param>
/// <param name="Trend">Per-bucket values across the period (empty for snapshot KPIs).</param>
public sealed record DashboardKpiDto(
    decimal Value,
    decimal? DeltaPct,
    int Count,
    decimal[] Trend);

/// <summary>The overdue KPI — like <see cref="DashboardKpiDto"/> plus the number of distinct partners.</summary>
/// <param name="Value">Total overdue receivable (aged 31+ days).</param>
/// <param name="DeltaPct">Always null — this is a current snapshot.</param>
/// <param name="Count">The number of overdue transactions.</param>
/// <param name="Trend">Empty — snapshot.</param>
/// <param name="PartnerCount">The number of distinct partners with overdue receivable.</param>
public sealed record DashboardOverdueKpiDto(
    decimal Value,
    decimal? DeltaPct,
    int Count,
    decimal[] Trend,
    int PartnerCount);

/// <summary>One time-bucket of the dashboard series.</summary>
/// <param name="Label">The bucket label (hour for "today", date for "week"/"month").</param>
/// <param name="Sales">Gross sales total in the bucket.</param>
/// <param name="Supplies">Gross supplies total in the bucket.</param>
/// <param name="Payin">Money received (Income payments) in the bucket.</param>
/// <param name="Payout">Money paid out (Expense payments) in the bucket.</param>
/// <param name="WalletPayin">Pay-in split per wallet, aligned to <see cref="DashboardDto.Wallets"/>.</param>
/// <param name="WalletPayout">Pay-out split per wallet, aligned to <see cref="DashboardDto.Wallets"/>.</param>
public sealed record DashboardSeriesPointDto(
    string Label,
    decimal Sales,
    decimal Supplies,
    decimal Payin,
    decimal Payout,
    decimal[] WalletPayin,
    decimal[] WalletPayout);

/// <summary>A wallet entry for the dashboard's payments-chart filter.</summary>
/// <param name="Id">The wallet id.</param>
/// <param name="Name">The wallet name.</param>
/// <param name="Type">The wallet type (Cash / Card / Bank).</param>
public sealed record DashboardWalletDto(
    int Id,
    string Name,
    string Type);

/// <summary>A receivable-aging bucket.</summary>
/// <param name="Bucket">The age range ("0-7" / "8-30" / "31-60" / "60+").</param>
/// <param name="Amount">Total receivable remaining in the bucket.</param>
public sealed record DashboardAgingBucketDto(
    string Bucket,
    decimal Amount);

/// <summary>A top-debtor row.</summary>
/// <param name="PartnerId">The partner id.</param>
/// <param name="Name">The partner's name.</param>
/// <param name="Company">The partner's company, if any.</param>
/// <param name="Amount">The partner's total receivable.</param>
public sealed record DashboardDebtorDto(
    int PartnerId,
    string Name,
    string? Company,
    decimal Amount);

/// <summary>A recent sale or supply for the dashboard list.</summary>
/// <param name="Id">The transaction id.</param>
/// <param name="Date">The transaction date.</param>
/// <param name="PartnerName">The partner's name.</param>
/// <param name="Type">"Sale" or "Supply".</param>
/// <param name="Total">The transaction's total due.</param>
/// <param name="Paid">How much has been settled.</param>
/// <param name="Status">Settlement status: "paid" / "partial" / "unpaid".</param>
public sealed record DashboardRecentTransactionDto(
    int Id,
    DateTimeOffset Date,
    string PartnerName,
    string Type,
    decimal Total,
    decimal Paid,
    string Status);
