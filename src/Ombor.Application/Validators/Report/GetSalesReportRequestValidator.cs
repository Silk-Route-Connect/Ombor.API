using FluentValidation;
using Ombor.Contracts.Requests.Report;

namespace Ombor.Application.Validators.Report;

/// <summary>The period itself (order, length, defaults) is checked where it is resolved, <c>ReportRange</c>, since its defaults depend on today's business date.</summary>
public sealed class GetSalesReportRequestValidator : AbstractValidator<GetSalesReportRequest>
{
    public GetSalesReportRequestValidator()
    {
        RuleFor(x => x.GroupBy)
            .IsInEnum()
            .When(x => x.GroupBy.HasValue)
            .WithMessage("Invalid grouping.");
    }
}
