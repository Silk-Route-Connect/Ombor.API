using FluentValidation;
using Ombor.Contracts.Requests.Payroll;

namespace Ombor.Application.Validators.Payroll;

public sealed class UpdatePayrollRequestValidator : AbstractValidator<UpdatePayrollRequest>
{
    public UpdatePayrollRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Currency cannot exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Exchange rate must be non-negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes cannot exceed {ValidationConstants.MaxStringLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("Invalid payment method.");
    }
}
