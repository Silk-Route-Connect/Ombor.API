using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Application.Validators.Partner;

public sealed class GetPartnersRequestValidator : PagedRequestValidator<GetPartnersRequest>
{
    public GetPartnersRequestValidator()
    {
        RuleFor(x => x.Type)
             .IsInEnum()
             .When(x => x.Type.HasValue)
             .WithMessage("Invalid partner type.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "name_asc",
                "name_desc",
                "companyname_asc",
                "companyname_desc",
                "balance_asc",
                "balance_desc",
                "email_asc",
                "email_desc",
                "address_asc",
                "address_desc",
                "type_asc",
                "type_desc");
    }
}
