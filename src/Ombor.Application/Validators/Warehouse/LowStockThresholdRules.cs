using FluentValidation;

namespace Ombor.Application.Validators.Warehouse;

internal static class LowStockThresholdRules
{
    /// <summary>
    /// The threshold rule shared by the stock-row threshold and the opening-stock line: optional, never negative, and
    /// stored like a stock quantity (decimal(18,3)), so a value the column cannot hold is a field error, not a 500.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal?> LowStockThreshold<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Low-stock threshold cannot be negative.")
            .PrecisionScale(18, 3, ignoreTrailingZeros: true)
            .WithMessage("Low-stock threshold must have at most 15 digits before the decimal point and 3 after it.");
}
