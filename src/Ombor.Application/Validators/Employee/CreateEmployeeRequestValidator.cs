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

        RuleFor(x => x.Salary)
            .GreaterThan(0)
            .WithMessage("Salary must be greater than zero.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email is in invalid format.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("Phone number is in invalid format.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Address must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Description must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Position)
            .IsInEnum()
            .WithMessage("Position must be a valid enum value.");

        RuleFor(x => x.DateOfEmployment)
            .NotEmpty()
            .WithMessage("Date of employment is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of employment cannot be in the future.");
    }
}
