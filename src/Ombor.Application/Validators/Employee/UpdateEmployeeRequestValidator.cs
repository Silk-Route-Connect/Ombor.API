using FluentValidation;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid employee ID.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Full name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Role)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Role must not exceed {ValidationConstants.DefaultStringLength} characters.");

    }
}
