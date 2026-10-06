using FluentValidation;
using Ombor.Application.Helpers;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .Must(PhoneNumbers.IsValid)
            .WithMessage("One or more phone numbers are in invalid format.");
    }
}
