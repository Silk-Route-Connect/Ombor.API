using FluentValidation;
using Ombor.Contracts.Requests.Report;

namespace Ombor.Application.Validators.Report;

/// <summary>The period itself (order, length, defaults) is checked where it is resolved, <c>ReportRange</c>, since its defaults depend on today's business date.</summary>
public sealed class GetReportPeriodRequestValidator : AbstractValidator<GetReportPeriodRequest>
{
}
