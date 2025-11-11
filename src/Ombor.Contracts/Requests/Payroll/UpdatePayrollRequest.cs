using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payroll;

public sealed record UpdatePayrollRequest(
    int PaymentId,
    int EmployeeId,
    decimal Amount,
    string Currency,
    decimal ExchangeRate,
    PaymentMethod Method,
    string? Notes);
