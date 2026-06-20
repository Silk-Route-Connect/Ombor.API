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

        RuleFor(x => x.WalletId)
            .GreaterThan(0)
            .WithMessage("WalletId must be greater than 0.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Period)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Period must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes must not exceed {ValidationConstants.MaxStringLength} characters.");
    }
}
