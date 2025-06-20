namespace Ombor.Contracts.Enums;

/// <summary>Direction of cash flow relative to the company.</summary>
public enum PaymentDirection
{
    /// <summary>
    /// Money comes <c>into</c> the company (sale, supply refund, etc.).
    /// </summary>
    Income = 1,

    /// <summary>
    /// Money goes <c>out of</c> the company (payroll, supply, supply refund, etc.).
    /// </summary>
    Expense = 2
}
