namespace Ombor.Application.Services;

/// <summary>
/// The debt directions surfaced in the debts read model. Shared so consumers (e.g. the dashboard
/// reusing the debt figures) match on the same literal the debt projection produces.
/// </summary>
internal static class DebtDirections
{
    /// <summary>The partner owes us (unpaid Sale / SupplyRefund).</summary>
    public const string Receivable = "Receivable";

    /// <summary>We owe the partner (unpaid Supply / SaleRefund).</summary>
    public const string Payable = "Payable";

    /// <summary>A partner whose unpaid documents are fully netted by an advance — nobody owes anybody.</summary>
    public const string Settled = "Settled";

    /// <summary>The direction of a signed net partner balance (positive = the partner owes us).</summary>
    public static string OfBalance(decimal balance) => balance > 0m ? Receivable : balance < 0m ? Payable : Settled;
}
