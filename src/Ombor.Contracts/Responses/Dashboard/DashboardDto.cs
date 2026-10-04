namespace Ombor.Contracts.Responses.Dashboard;

/// <summary>
/// The dashboard read model — an aggregated snapshot for the current organization, on the business (Tashkent) calendar.
/// Debt figures (receivable/payable/overdue/aging/top-debtors) are net partner positions — the same figures as
/// <c>GET /api/debts/summary</c> and the partner balances; their values are today's, their trends run across the
/// period. Every KPI with a trend has one value per <see cref="Series"/> bucket.
/// </summary>
/// <param name="BusinessName">The organization's name.</param>
/// <param name="Period">The requested period ("today" / "week" / "month").</param>
/// <param name="Revenue">Sales net of sale refunds in the period; count = sales documents.</param>
/// <param name="Receivable">What partners owe us — Σ positive net partner balances; count = partners who owe us.</param>
/// <param name="Payable">What we owe partners — Σ |negative net partner balances|; count = partners we owe.</param>
/// <param name="Overdue">The part of <see cref="Receivable"/> older than 30 days (the aging definition, distinct from due-date overdue).</param>
/// <param name="Series">Per-bucket sales/supplies/refunds/pay-in/pay-out time series for the period.</param>
/// <param name="Wallets">Wallets, for the payments chart's per-wallet filter (aligned to the series wallet arrays).</param>
/// <param name="Aging">The receivable bucketed by age; the buckets add up to <see cref="Receivable"/>.</param>
/// <param name="TopDebtors">Partners with the largest net receivable.</param>
/// <param name="RecentTransactions">The most recent sales and supplies.</param>
/// <param name="SaleRefunds">Sale refunds in the period (already netted out of <see cref="Revenue"/>); count = refund documents.</param>
/// <param name="StockValue">The carrying value of all stock (Σ quantity × WAC), archived warehouses included (rule 31).</param>
/// <param name="Cash">Money in all wallets, archived ones included (rule 31), with a trend and the per-wallet balances.</param>
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
    DashboardRecentTransactionDto[] RecentTransactions,
    DashboardKpiDto SaleRefunds,
    DashboardStockValueDto StockValue,
    DashboardCashDto Cash);

/// <summary>The value of the stock on hand.</summary>
/// <param name="Value">Σ quantity × weighted-average cost over every warehouse, rounded to 2 decimals.</param>
/// <param name="ProductCount">Distinct products in stock.</param>
/// <param name="WarehouseCount">Warehouses holding stock.</param>
public sealed record DashboardStockValueDto(
    decimal Value,
    int ProductCount,
    int WarehouseCount);

/// <summary>Money on hand across wallets.</summary>
/// <param name="Value">Σ wallet balances (archived wallets included).</param>
/// <param name="DeltaPct">Percent change vs the start of the period; null when that was 0.</param>
/// <param name="Trend">Cash at the end of each series bucket, ending on <see cref="Value"/>.</param>
/// <param name="Wallets">Each wallet's balance, by name.</param>
public sealed record DashboardCashDto(
    decimal Value,
    decimal? DeltaPct,
    decimal[] Trend,
    DashboardWalletBalanceDto[] Wallets);

/// <summary>A wallet and its computed balance.</summary>
/// <param name="Id">The wallet id.</param>
/// <param name="Name">The wallet name.</param>
/// <param name="Type">Cash / Card / Bank.</param>
/// <param name="Balance">The computed balance (the same figure as <c>WalletDto.Balance</c>).</param>
/// <param name="IsArchived">Whether the wallet is archived (it still counts).</param>
public sealed record DashboardWalletBalanceDto(
    int Id,
    string Name,
    string Type,
    decimal Balance,
    bool IsArchived);

/// <summary>A headline figure with its change and a per-bucket trend.</summary>
/// <param name="Value">The headline amount.</param>
/// <param name="DeltaPct">
/// Percent change — for period flows (revenue, refunds) vs the preceding equal period, for positions (receivable,
/// payable) vs the start of the period; null when the basis is 0.
/// </param>
/// <param name="Count">The number of items behind the value (documents for flows, partners for positions).</param>
/// <param name="Trend">Per-bucket values across the period, aligned to the series: the flow within each bucket, or the position at each bucket's end (the last equals <see cref="Value"/>).</param>
public sealed record DashboardKpiDto(
    decimal Value,
    decimal? DeltaPct,
    int Count,
    decimal[] Trend);

/// <summary>The «старше 30 дней» KPI — like <see cref="DashboardKpiDto"/> plus the number of distinct partners.</summary>
/// <param name="Value">The part of the net receivable aged 31+ days.</param>
/// <param name="DeltaPct">Percent change vs the start of the period; null when that was 0.</param>
/// <param name="Count">Documents (or opening balances) carrying that part.</param>
/// <param name="Trend">The aged-31+ amount at the end of each series bucket, ending on <see cref="Value"/>.</param>
/// <param name="PartnerCount">The number of distinct partners with receivable aged 31+ days.</param>
public sealed record DashboardOverdueKpiDto(
    decimal Value,
    decimal? DeltaPct,
    int Count,
    decimal[] Trend,
    int PartnerCount);

/// <summary>One time-bucket of the dashboard series.</summary>
/// <param name="Label">The bucket label: local hour "00"–"23" for "today", local date "yyyy-MM-dd" for "week"/"month".</param>
/// <param name="Sales">Gross sales total in the bucket.</param>
/// <param name="Supplies">Gross supplies total in the bucket.</param>
/// <param name="Payin">Money received (Income payments) in the bucket.</param>
/// <param name="Payout">Money paid out (Expense payments) in the bucket.</param>
/// <param name="WalletPayin">Pay-in split per wallet, aligned to <see cref="DashboardDto.Wallets"/>.</param>
/// <param name="WalletPayout">Pay-out split per wallet, aligned to <see cref="DashboardDto.Wallets"/>.</param>
/// <param name="SaleRefunds">Sale refunds total in the bucket (revenue = sales − sale refunds).</param>
public sealed record DashboardSeriesPointDto(
    string Label,
    decimal Sales,
    decimal Supplies,
    decimal Payin,
    decimal Payout,
    decimal[] WalletPayin,
    decimal[] WalletPayout,
    decimal SaleRefunds);

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
/// <param name="Amount">The part of the net receivable this old.</param>
public sealed record DashboardAgingBucketDto(
    string Bucket,
    decimal Amount);

/// <summary>A top-debtor row.</summary>
/// <param name="PartnerId">The partner id.</param>
/// <param name="Name">The partner's name.</param>
/// <param name="Company">The partner's company, if any.</param>
/// <param name="Amount">The partner's net receivable (its positive balance).</param>
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
/// <param name="TransactionNumber">The transaction's bare document number (the client prepends «№»); null for a legacy row without one.</param>
/// <param name="PartnerId">The partner id, to link the partner.</param>
public sealed record DashboardRecentTransactionDto(
    int Id,
    DateTimeOffset Date,
    string PartnerName,
    string Type,
    decimal Total,
    decimal Paid,
    string Status,
    string? TransactionNumber,
    int PartnerId);
