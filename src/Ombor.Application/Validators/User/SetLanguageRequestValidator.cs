using FluentValidation;
using Ombor.Contracts.Requests.User;

namespace Ombor.Application.Validators.User;

public sealed class SetLanguageRequestValidator : AbstractValidator<SetLanguageRequest>
{
    private static readonly string[] AllowedLanguages = ["ru", "uz-Latn", "uz-Cyrl"];

    public SetLanguageRequestValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(language => AllowedLanguages.Contains(language))
            .WithMessage("Language must be one of: ru, uz-Latn, uz-Cyrl.");
    }
}
