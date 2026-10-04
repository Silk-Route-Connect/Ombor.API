namespace Ombor.Contracts.Responses.Debt;

/// <summary>
/// Who owes whom, organization-wide — the one source for every «Нам должны» / «Мы должны» figure (Partners summary
/// strip, Debts cards, Dashboard KPIs). A partner's debt is its net ledger balance (the same figure as
/// <c>PartnerDto.Balance</c>): opening balance + unpaid documents ± advances. A partner whose advance exceeds an
/// unpaid sale owes nothing — we owe them. Archived partners count (rule 31).
/// </summary>
/// <param name="Receivable">Σ positive net partner balances — what partners owe us.</param>
/// <param name="ReceivablePartnerCount">Partners who owe us.</param>
/// <param name="Payable">Σ |negative net partner balances| — what we owe partners.</param>
/// <param name="PayablePartnerCount">Partners we owe.</param>
/// <param name="Net"><see cref="Receivable"/> − <see cref="Payable"/>.</param>
/// <param name="OlderThan30Days">The part of <see cref="Receivable"/> older than 30 days (the aging definition).</param>
/// <param name="Aging">The receivable by age; the buckets add up to <see cref="Receivable"/>.</param>
/// <param name="UnpaidDocuments">Document-level totals: what the unpaid documents themselves still claim, before advances net them.</param>
/// <param name="Partners">Every partner with a non-zero balance or an unpaid document, largest amount first.</param>
public sealed record DebtSummaryDto(
    decimal Receivable,
    int ReceivablePartnerCount,
    decimal Payable,
    int PayablePartnerCount,
    decimal Net,
    decimal OlderThan30Days,
    DebtAgingBucketDto[] Aging,
    DebtDocumentTotalsDto UnpaidDocuments,
    DebtPartnerPositionDto[] Partners);

/// <summary>A receivable age bucket.</summary>
/// <param name="Bucket">"0-7" / "8-30" / "31-60" / "60+" (days).</param>
/// <param name="Amount">The part of the receivable this old.</param>
public sealed record DebtAgingBucketDto(string Bucket, decimal Amount);

/// <summary>
/// Totals over the unpaid documents (the <c>GET /api/debts</c> list). These are «Неоплаченные документы», not «долг»:
/// an advance the partner holds is not netted here.
/// </summary>
/// <param name="Receivable">Σ remaining of unpaid Sale / SupplyRefund documents.</param>
/// <param name="ReceivableCount">How many such documents.</param>
/// <param name="Payable">Σ remaining of unpaid Supply / SaleRefund documents.</param>
/// <param name="PayableCount">How many such documents.</param>
/// <param name="PastDue">Σ remaining of unpaid documents past their due date (both directions).</param>
/// <param name="PastDueCount">How many such documents.</param>
public sealed record DebtDocumentTotalsDto(
    decimal Receivable,
    int ReceivableCount,
    decimal Payable,
    int PayableCount,
    decimal PastDue,
    int PastDueCount);

/// <summary>One partner's position and what it is made of.</summary>
/// <param name="PartnerId">The partner id.</param>
/// <param name="Name">The partner's name.</param>
/// <param name="Company">The partner's company, if any.</param>
/// <param name="PartnerType">Customer / Supplier / Both.</param>
/// <param name="IsArchived">Whether the partner is archived (it still counts).</param>
/// <param name="Direction">"Receivable" (owes us), "Payable" (we owe) or "Settled" (unpaid documents fully netted by an advance).</param>
/// <param name="Balance">The signed net balance — positive = the partner owes us; equals <c>PartnerDto.Balance</c>.</param>
/// <param name="Amount">|<see cref="Balance"/>|.</param>
/// <param name="OpeningBalance">The signed opening balance included in <see cref="Balance"/>.</param>
/// <param name="UnpaidReceivable">Σ remaining of the partner's unpaid Sale / SupplyRefund documents.</param>
/// <param name="UnpaidPayable">Σ remaining of the partner's unpaid Supply / SaleRefund documents.</param>
/// <param name="UnpaidDocumentCount">How many unpaid documents the partner has.</param>
/// <param name="PartnerAdvance">Money the partner paid in advance that we hold (lowers what they owe).</param>
/// <param name="CompanyAdvance">Money we paid the partner in advance (raises what they owe).</param>
/// <param name="OldestAgeDays">Age of the oldest part of what the partner owes us; null when they owe nothing.</param>
public sealed record DebtPartnerPositionDto(
    int PartnerId,
    string Name,
    string? Company,
    string PartnerType,
    bool IsArchived,
    string Direction,
    decimal Balance,
    decimal Amount,
    decimal OpeningBalance,
    decimal UnpaidReceivable,
    decimal UnpaidPayable,
    int UnpaidDocumentCount,
    decimal PartnerAdvance,
    decimal CompanyAdvance,
    int? OldestAgeDays);
