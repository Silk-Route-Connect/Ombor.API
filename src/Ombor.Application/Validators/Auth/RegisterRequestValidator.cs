using FluentValidation;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("FirstName is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"FirstName must not exceed {ValidationConstants.DefaultStringLength}characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("LastName is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"LastName must not exceed {ValidationConstants.DefaultStringLength}characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Password must not exceed {ValidationConstants.DefaultStringLength}characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm Password is required.")
            .Equal(x => x.Password)
            .WithMessage("Passwords and Confirm Password do not match.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("One or more phone numbers are in invalid format.");

        RuleFor(x => x.Email)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .EmailAddress()
            .WithMessage("Invalid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.TelegramAccount)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"TelegramAccount must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.TelegramAccount));

        RuleFor(x => x.OrganizationName)
            .NotEmpty()
            .WithMessage("OrganizationName is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"OrganizationName must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.OrganizationName));
    }
}
