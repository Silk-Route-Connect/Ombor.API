using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// Money paid out of wallets in a period (expense payments), by type and by who received it. The total equals the
/// cash-flow report's expense for the same days.
/// </summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="Total">Everything paid out.</param>
/// <param name="Count">Expense payments.</param>
/// <param name="ByType">By payment type (every type, zeros included).</param>
/// <param name="Partners">Payments to partners — suppliers paid, refunds and advances paid out (types Transaction / Deposit / Withdrawal), largest first.</param>
/// <param name="Employees">Payroll by employee, largest first.</param>
/// <param name="Other">Other (General) expenses grouped by their description, largest first.</param>
public sealed record ExpensesReportDto(
    DateOnly From,
    DateOnly To,
    decimal Total,
    int Count,
    ExpenseTypeDto[] ByType,
    ExpensePartnerDto[] Partners,
    ExpenseEmployeeDto[] Employees,
    ExpenseOtherDto[] Other);

/// <summary>Expense payments of one type.</summary>
/// <param name="Type">The payment type.</param>
/// <param name="Amount">Paid out.</param>
/// <param name="Count">Payments.</param>
public sealed record ExpenseTypeDto(
    PaymentType Type,
    decimal Amount,
    int Count);

/// <summary>Expense payments to one partner.</summary>
/// <param name="PartnerId">The partner id.</param>
/// <param name="Name">The partner name.</param>
/// <param name="Amount">Paid out.</param>
/// <param name="Count">Payments.</param>
public sealed record ExpensePartnerDto(
    int PartnerId,
    string Name,
    decimal Amount,
    int Count);

/// <summary>Payroll paid to one employee.</summary>
/// <param name="EmployeeId">The employee id.</param>
/// <param name="Name">The employee's full name.</param>
/// <param name="Amount">Paid out.</param>
/// <param name="Count">Payroll payments.</param>
public sealed record ExpenseEmployeeDto(
    int EmployeeId,
    string Name,
    decimal Amount,
    int Count);

/// <summary>Other (General) expenses with the same description (trimmed, case-insensitive).</summary>
/// <param name="Description">The description as first entered; null when the payments have none.</param>
/// <param name="Amount">Paid out.</param>
/// <param name="Count">Payments.</param>
public sealed record ExpenseOtherDto(
    string? Description,
    decimal Amount,
    int Count);
