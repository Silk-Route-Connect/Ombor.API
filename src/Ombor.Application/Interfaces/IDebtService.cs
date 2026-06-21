using Ombor.Contracts.Responses.Debt;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Builds the debts read model by deriving it on read from the transaction ledger. Nothing is stored;
/// the figures reconcile with the partner balance view (same <c>TotalDue − TotalPaid</c> source).
/// </summary>
public interface IDebtService
{
    /// <summary>
    /// All unpaid/partially-paid transactions of the current organization, newest-first, one row each.
    /// </summary>
    Task<DebtDto[]> GetDebtsAsync();
}
