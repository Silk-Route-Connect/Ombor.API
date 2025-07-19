using FluentValidation;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Full name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is requieed.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Role must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
