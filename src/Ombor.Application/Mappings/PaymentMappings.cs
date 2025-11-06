using Ombor.Contracts.Requests.Payroll;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class PaymentMappings
{
    public static Payment ToPaymentEntity(this CreatePayrollRequest request)
        => new()
        {
            EmployeeId = request.EmployeeId,
            Notes = request.Notes,
            DateUtc = DateTime.UtcNow,
            Direction = Domain.Enums.PaymentDirection.Expense,
            Type = Domain.Enums.PaymentType.Payroll,
            Components =
            [
                new()
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ExchangeRate = request.ExchangeRate,
                    Method = Enum.Parse<Domain.Enums.PaymentMethod>(request.Method.ToString(), ignoreCase: true),
                    Payment = null! // Will be set by EF
                }
            ]
        };

    public static void ApplyUpdate(this Payment payment, UpdatePayrollRequest request)
    {
        payment.Notes = request.Notes;
        payment.Components =
        [
            new() {
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Method = Enum.Parse<Domain.Enums.PaymentMethod>(request.Method.ToString(), ignoreCase: true),
                Payment = null! // Will be set by EF
            }
        ];
    }
}
