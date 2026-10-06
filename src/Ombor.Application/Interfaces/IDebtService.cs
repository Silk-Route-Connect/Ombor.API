using Ombor.Contracts.Responses.Debt;

namespace Ombor.Application.Interfaces;

/// <summary>
/// The debts read models, derived on read from the ledger — nothing is stored. The document list answers «which
/// documents are unpaid»; the summary answers «who owes whom, how much» from net partner balances, the one source
/// every debt total on every page uses.
/// </summary>
public interface IDebtService
{
    /// <summary>
    /// All unpaid/partially-paid transactions of the current organization, newest-first, one row each.
    /// </summary>
    Task<DebtDto[]> GetDebtsAsync();

    /// <summary>Net partner positions, the receivable/payable totals and aging, and the unpaid-document totals.</summary>
    Task<DebtSummaryDto> GetSummaryAsync();
}
