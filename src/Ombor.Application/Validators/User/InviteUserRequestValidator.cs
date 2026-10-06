using FluentValidation;
using Ombor.Application.Helpers;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.User;

namespace Ombor.Application.Validators.User;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Contact value is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Contact value must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Value)
            .Must(PhoneNumbers.IsValid)
            .WithMessage("One or more phone numbers are in invalid format.")
            .When(x => x.Method == ContactType.Phone && !string.IsNullOrWhiteSpace(x.Value));

        RuleFor(x => x.FirstName)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"FirstName must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"LastName must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
