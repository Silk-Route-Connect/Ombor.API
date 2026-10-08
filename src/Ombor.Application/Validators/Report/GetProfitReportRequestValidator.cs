using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Report;

namespace Ombor.Application.Validators.Report;

/// <summary>The period itself (order, length, defaults) is checked where it is resolved, <c>ReportRange</c>, since its defaults depend on today's business date.</summary>
public sealed class GetProfitReportRequestValidator : AbstractValidator<GetProfitReportRequest>
{
    public GetProfitReportRequestValidator()
    {
        RuleFor(x => x.GroupBy)
            .Must(g => g is null or ReportGroupBy.Day or ReportGroupBy.Week or ReportGroupBy.Month)
            .WithMessage("The profit report groups by Day, Week or Month.");
    }
}
