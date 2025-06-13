using FluentValidation;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Application.Validators.Partner;

public sealed class CreatePartnerRequestValidator : AbstractValidator<CreatePartnerRequest>
{
    public CreatePartnerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Address must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Email)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Company name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleForEach(x => x.PhoneNumbers)
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("The phone number is in the wrong format.");
    }
}
