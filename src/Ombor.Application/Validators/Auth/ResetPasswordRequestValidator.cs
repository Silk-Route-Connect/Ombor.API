using FluentValidation;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("One or more phone numbers are in invalid format.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Password must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm Password is required.")
            .Equal(x => x.NewPassword)
            .WithMessage("The password and confirmation password do not match.");
    }
}
