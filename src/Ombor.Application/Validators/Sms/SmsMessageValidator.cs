using FluentValidation;
using Ombor.Application.Models;

namespace Ombor.Application.Validators.Sms;

public sealed class SmsMessageValidator : AbstractValidator<SmsMessage>
{
    public SmsMessageValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message cannot be empty.");

        RuleFor(x => x.ToNumber)
            .NotEmpty()
            .WithMessage("Phone number cannot be empty.")
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("One or more phone numbers are in invalid format.");
    }
}
