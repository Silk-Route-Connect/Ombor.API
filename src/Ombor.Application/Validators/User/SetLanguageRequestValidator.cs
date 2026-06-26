using FluentValidation;
using Ombor.Application.Localization;
using Ombor.Contracts.Requests.User;

namespace Ombor.Application.Validators.User;

public sealed class SetLanguageRequestValidator : AbstractValidator<SetLanguageRequest>
{
    public SetLanguageRequestValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(SupportedLanguages.IsSupported)
            .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages.All)}.");
    }
}
