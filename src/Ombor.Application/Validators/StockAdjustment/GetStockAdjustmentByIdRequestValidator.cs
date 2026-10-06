using FluentValidation;
using Ombor.Contracts.Requests.StockAdjustment;

namespace Ombor.Application.Validators.StockAdjustment;

public sealed class GetStockAdjustmentByIdRequestValidator : AbstractValidator<GetStockAdjustmentByIdRequest>
{
    public GetStockAdjustmentByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid stock adjustment ID: {x.Id}.");
    }
}
