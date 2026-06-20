namespace Ombor.Contracts.Requests.Payroll;

/// <summary>
/// Records a payroll payment to an employee. The amount is drawn from <see cref="WalletId"/>
/// (an Expense). Payroll is immutable — corrections are reverse payments, not edits.
/// </summary>
/// <param name="EmployeeId">The employee being paid.</param>
/// <param name="WalletId">The wallet the money moves out of.</param>
/// <param name="Amount">The amount paid (UZS).</param>
/// <param name="Period">The payroll period, e.g. «2026-06».</param>
/// <param name="Notes">Optional free-text note.</param>
public sealed record CreatePayrollRequest(
    int EmployeeId,
    int WalletId,
    decimal Amount,
    string? Period,
    string? Notes);
