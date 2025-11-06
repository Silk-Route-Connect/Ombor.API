namespace Ombor.Contracts.Requests.Payroll;

public sealed record DeletePayrollRequest(int EmployeeId, int PaymentId);
