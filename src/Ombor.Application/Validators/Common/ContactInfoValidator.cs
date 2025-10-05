using FluentValidation;
using Ombor.Contracts.Common;

namespace Ombor.Application.Validators.Common;

public sealed class ContactInfoValidator : AbstractValidator<ContactInfo>
{
    public ContactInfoValidator()
    {
        RuleForEach(x => x.PhoneNumbers)
            .NotEmpty()
            .WithMessage("Phone number cannot be empty.")
            .MaximumLength(ValidationConstants.PhoneNumberLength)
            .WithMessage($"Phone number cannot exceed {ValidationConstants.PhoneNumberLength} characters.")
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("Phone number is in invalid format.")
            .When(x => x.PhoneNumbers.Length > 0);

        RuleFor(x => x.Email)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Address must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.TelegramAccount)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Telegram account must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
