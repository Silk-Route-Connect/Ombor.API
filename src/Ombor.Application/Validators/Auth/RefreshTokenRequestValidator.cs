using FluentValidation;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("RefreshToken is required.")
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"RefreshToken must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
