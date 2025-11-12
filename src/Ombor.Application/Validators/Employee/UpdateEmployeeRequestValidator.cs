using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid employee ID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Full name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Position)
            .NotEmpty()
            .WithMessage("Employee position is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Position must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Salary)
            .NotEmpty()
            .WithMessage("Salary is required.")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Salary must be positive.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid enum value.");

        RuleFor(x => x.DateOfEmployment)
            .NotEmpty()
            .WithMessage("Date of employment is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of employment cannot be in the future.");

        RuleFor(x => x.ContactInfo!)
            .SetValidator(new ContactInfoValidator())
            .When(x => x.ContactInfo is not null);
    }
}
