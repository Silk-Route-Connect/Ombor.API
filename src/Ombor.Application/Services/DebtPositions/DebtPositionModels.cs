using Ombor.Domain.Enums;

namespace Ombor.Application.Services.DebtPositions;

/// <summary>
/// A point in time to measure positions at: every event dated before <see cref="Before"/> counts, and ages are taken
/// as of the local date <see cref="AsOf"/>.
/// </summary>
internal sealed record DebtCutoff(DateTimeOffset Before, DateOnly AsOf);

/// <summary>A part of a partner's net receivable with the date it dates from.</summary>
/// <param name="Amount">The portion of the net receivable.</param>
/// <param name="AgeDays">Whole local days since the item it is attributed to.</param>
/// <param name="TransactionId">The unpaid document it sits on; null for the opening balance or an undated remainder.</param>
internal sealed record AgedPortion(decimal Amount, int AgeDays, int? TransactionId);

/// <summary>One partner's position now: the signed net balance (the ledger figure) and what makes it up.</summary>
internal sealed record PartnerPosition(
    int PartnerId,
    string Name,
    string? Company,
    PartnerType Type,
    bool IsArchived,
    decimal Balance,
    decimal OpeningBalance,
    decimal PartnerAdvance,
    decimal CompanyAdvance,
    decimal UnpaidReceivable,
    decimal UnpaidPayable,
    int UnpaidDocumentCount,
    IReadOnlyList<AgedPortion> AgedReceivable);

/// <summary>Organization-wide debt totals at one point in time.</summary>
/// <param name="Receivable">Σ positive net partner balances — what partners owe us.</param>
/// <param name="ReceivablePartners">Partners whose net balance is positive.</param>
/// <param name="Payable">Σ |negative net partner balances| — what we owe partners.</param>
/// <param name="PayablePartners">Partners whose net balance is negative.</param>
/// <param name="OlderThan30Days">The part of <see cref="Receivable"/> aged 31+ days.</param>
/// <param name="OlderThan30DaysItems">Documents (or opening balances) carrying that part.</param>
/// <param name="OlderThan30DaysPartners">Partners carrying that part.</param>
/// <param name="Aging">The receivable split into the age buckets of <see cref="DebtAging.Buckets"/>.</param>
internal sealed record DebtTotals(
    decimal Receivable,
    int ReceivablePartners,
    decimal Payable,
    int PayablePartners,
    decimal OlderThan30Days,
    int OlderThan30DaysItems,
    int OlderThan30DaysPartners,
    decimal[] Aging);

/// <summary>The result of one calculation: positions and totals now, plus totals at each requested cutoff.</summary>
internal sealed record DebtPositionSnapshot(
    IReadOnlyList<PartnerPosition> Partners,
    DebtTotals Totals,
    IReadOnlyList<DebtTotals> AtCutoffs);
