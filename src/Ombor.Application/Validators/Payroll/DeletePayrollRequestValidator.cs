using FluentValidation;
using Ombor.Contracts.Requests.Payroll;

namespace Ombor.Application.Validators.Payroll;

public sealed class DeletePayrollRequestValidator : AbstractValidator<DeletePayrollRequest>
{
    public DeletePayrollRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid Employee ID: {x.EmployeeId}.");

        RuleFor(x => x.PaymentId)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid Payment ID: {x.PaymentId}.");
    }
}
