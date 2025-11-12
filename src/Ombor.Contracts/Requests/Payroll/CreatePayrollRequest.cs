using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payroll;

public sealed record CreatePayrollRequest(
    int EmployeeId,
    decimal Amount,
    string Currency,
    decimal ExchangeRate,
    string? Notes,
    PaymentMethod Method);
