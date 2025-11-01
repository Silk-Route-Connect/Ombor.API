using FluentValidation;
using FluentValidation.Results;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Inventory;

namespace Ombor.Application.Validators.Inventory;

public sealed class GetInventoriesRequestValidator : PagedRequestValidator<GetInventoriesRequest>
{
    public GetInventoriesRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "name_asc",
                "name_desc",
                "location_asc",
                "location_desc");
    }

    protected override bool PreValidate(ValidationContext<GetInventoriesRequest> context, ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.InstanceToValidate);

        return base.PreValidate(context, result);
    }
}
