using FluentValidation;
using Ombor.Contracts.Requests.Payroll;

namespace Ombor.Application.Validators.Payroll;

public sealed class CreatePayrollRequestValidator : AbstractValidator<CreatePayrollRequest>
{
    public CreatePayrollRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("EmployeeId must be greater than 0.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .MaximumLength(3)
            .WithMessage("Currency must not exceed 3 characters.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ExchangeRate must be non-negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage("Notes must not exceed 500 characters.");

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("Invalid payment method.");
    }
}
