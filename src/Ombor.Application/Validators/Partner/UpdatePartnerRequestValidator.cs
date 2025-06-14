using FluentValidation;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Application.Validators.Partner;

public sealed class UpdatePartnerRequestValidator : AbstractValidator<UpdatePartnerRequest>
{
    public UpdatePartnerRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid partner ID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Description must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Email)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .EmailAddress()
            .WithMessage("Invalid email address.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Company name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleForEach(x => x.PhoneNumbers)
            .Must(ValidationHelpers.IsValidPhoneNumber)
            .WithMessage("The phone number is in the wrong format.");
    }
}
