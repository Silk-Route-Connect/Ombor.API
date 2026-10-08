using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Application.Validators.Warehouse;

public sealed class SetLowStockThresholdRequestValidator : AbstractValidator<SetLowStockThresholdRequest>
{
    public SetLowStockThresholdRequestValidator()
    {
        RuleFor(x => x.LowStockThreshold)
            .LowStockThreshold();
    }
}
