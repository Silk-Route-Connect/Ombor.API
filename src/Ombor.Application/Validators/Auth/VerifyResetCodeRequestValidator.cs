using FluentValidation;
using Ombor.Application.Helpers;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class VerifyResetCodeRequestValidator : AbstractValidator<VerifyResetCodeRequest>
{
    public VerifyResetCodeRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .Must(PhoneNumbers.IsValid)
            .WithMessage("One or more phone numbers are in invalid format.");

        // Only require the code to be present here — a present-but-wrong code returns success:false (200),
        // it is not a malformed request.
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required.");
    }
}
