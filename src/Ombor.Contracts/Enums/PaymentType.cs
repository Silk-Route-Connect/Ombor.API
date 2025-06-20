namespace Ombor.Contracts.Enums;

/// <summary>
/// Business purpose for recording the payment.
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// Payment that settles one or more commercial transactions (Sale, Supply, etc).
    /// </summary>
    Transaction = 1,

    /// <summary>
    /// Top-up that increases the partner’s balance.
    /// </summary>
    Deposit = 2,

    /// <summary>
    /// Cash-out that decreases the partner’s balance.
    /// </summary>
    Withdrawal = 3,

    /// <summary>
    /// Salary or wage payment to an employee.
    /// </summary>
    Payroll = 4,

    /// <summary>
    /// Standalone income or expense such as rent or utilities.
    /// </summary>
    General = 5
}
