using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Application.Validators.Transaction;

public sealed class GetTransactionsRequestValidator : PagedRequestValidator<GetTransactionsRequest>
{
    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.PartnerId)
            .GreaterThan(0)
            .When(x => x.PartnerId.HasValue)
            .WithMessage("Partner ID must be greater than zero.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate ?? DateTime.MinValue)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.FromDate.HasValue)
            .WithMessage("Start date cannot be in the future.");

        RuleFor(x => x.SearchTerm)
           .MaximumLength(100)
           .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
           .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "date_asc",
                "date_desc",
                "totalpaid_asc",
                "totalpaid_desc",
                "totaldue_asc",
                "totaldue_desc",
                "type_asc",
                "type_desc",
                "status_asc",
                "status_desc",
                "partnername_asc",
                "partnername_desc")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Invalid sort option.");
    }
}
