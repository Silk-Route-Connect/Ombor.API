using FluentValidation;
using Ombor.Application.Helpers;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.Application.Validators.Auth;

public sealed class SmsVerificationRequestValidator : AbstractValidator<SmsVerificationRequest>
{
    public SmsVerificationRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .Must(PhoneNumbers.IsValid)
            .WithMessage("One or more phone numbers are in invalid format.");

        // Presence only — a present-but-wrong code is answered with a code_invalid result, not a malformed request.
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required.")
            .MaximumLength(ValidationConstants.CodeLength)
            .WithMessage($"Code must not exceed {ValidationConstants.CodeLength} characters.");
    }
}
