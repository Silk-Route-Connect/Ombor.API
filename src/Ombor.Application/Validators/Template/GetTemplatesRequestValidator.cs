using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Template;

namespace Ombor.Application.Validators.Template;

public sealed class GetTemplatesRequestValidator : PagedRequestValidator<GetTemplatesRequest>
{
    public GetTemplatesRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
           .MaximumLength(100)
           .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
           .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "name_asc",
                "name_desc",
                "type_asc",
                "type_desc");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Invalid template type.");
    }
}
