using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Category;

namespace Ombor.Application.Validators.Category;

public sealed class GetCategoriesRequestValidator : PagedRequestValidator<GetCategoriesRequest>
{
    public GetCategoriesRequestValidator()
    {

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
            "name_asc",
            "name_desc",
            "desctiption_asc",
            "desctiption_desc")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Invalid sort option.");
    }
}
