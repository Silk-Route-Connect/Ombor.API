using FluentValidation;
using Ombor.Contracts.Requests.Report;

namespace Ombor.Application.Validators.Report;

public sealed class GetStockReportRequestValidator : AbstractValidator<GetStockReportRequest>
{
    public GetStockReportRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0)
            .When(x => x.WarehouseId.HasValue)
            .WithMessage(x => $"Invalid warehouse ID: {x.WarehouseId}.");
    }
}
