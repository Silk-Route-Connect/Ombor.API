namespace Ombor.Application.Services;

/// <summary>
/// The stock-movement kinds surfaced in the movement ledgers. Shared so consumers (e.g. the stock-adjustment
/// list reusing the ledger's running balance) match on the same literal the ledger produces.
/// </summary>
internal static class MovementKinds
{
    public const string Opening = "Opening";
    public const string Supply = "Supply";
    public const string Sale = "Sale";
    public const string Refund = "Refund";
    public const string Adjustment = "Adjustment";
    public const string Transfer = "Transfer";
}
