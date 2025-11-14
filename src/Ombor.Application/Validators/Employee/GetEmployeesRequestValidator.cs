using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class GetEmployeesRequestValidator : PagedRequestValidator<GetEmployeesRequest>
{
    public GetEmployeesRequestValidator()
    {
        RuleFor(x => x.Status)
           .IsInEnum()
           .When(x => x.Status.HasValue)
           .WithMessage("Invalid employee status.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "name_asc",
                "name_desc",
                "position_asc",
                "position_desc",
                "salary_asc",
                "salary_desc",
                "hiredate_asc",
                "hiredate_desc",
                "status_asc",
                "status_desc")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Invalid sort option.");
    }
}
