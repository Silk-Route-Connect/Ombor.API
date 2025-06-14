using FluentValidation;
using Ombor.Contracts.Requests.Template;

namespace Ombor.Application.Validators.Template;

public sealed class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Items)
            .Must(x => x.Length > 0)
            .WithMessage("Template items cannot be empty.");
    }
}
