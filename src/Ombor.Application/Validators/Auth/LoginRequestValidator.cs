using FluentValidation;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

/// <summary>
/// Presence and size only. A malformed or unknown phone is deliberately not a 400: it gets the same 401 as a wrong
/// password, so the response never tells which numbers have an account.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"PhoneNumber must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Password must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
