using FluentValidation;
using Ombor.Contracts.Requests.Organization;

namespace Ombor.Application.Validators.Organization;

public sealed class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Organization name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Organization name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Address must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(ValidationConstants.PhoneNumberLength)
            .WithMessage($"Phone must not exceed {ValidationConstants.PhoneNumberLength} characters.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
