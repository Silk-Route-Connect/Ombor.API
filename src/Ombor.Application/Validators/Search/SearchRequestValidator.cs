using FluentValidation;
using Ombor.Contracts.Requests.Search;

namespace Ombor.Application.Validators.Search;

public sealed class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public const int MaxQueryLength = 100;
    public const int MaxLimit = 20;

    public SearchRequestValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty()
            .WithMessage("Enter something to search for.")
            .MaximumLength(MaxQueryLength)
            .WithMessage($"The search text must not exceed {MaxQueryLength} characters.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"The limit must be between 1 and {MaxLimit}.");
    }
}
