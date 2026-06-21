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
}
