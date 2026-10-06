using FluentValidation;
using Ombor.Contracts.Requests.User;

namespace Ombor.Application.Validators.User;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Current password must not exceed {ValidationConstants.DefaultStringLength} characters.");

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
