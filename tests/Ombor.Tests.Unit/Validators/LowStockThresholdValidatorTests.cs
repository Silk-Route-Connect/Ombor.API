using System.Globalization;
using Ombor.Application.Validators.Warehouse;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Tests.Unit.Validators;

public sealed class LowStockThresholdValidatorTests
{
    private readonly SetLowStockThresholdRequestValidator _setValidator = new();
    private readonly AddOpeningStockValidator _openingValidator = new();

    [Theory]
    [InlineData(null, true)]                       // clears the threshold: the row is not tracked
    [InlineData("0", true)]
    [InlineData("2.5", true)]
    [InlineData("10.125", true)]                   // grams precision, like stock
    [InlineData("10.1250", true)]                  // trailing zeros are not extra precision
    [InlineData("999999999999999.999", true)]      // the largest value decimal(18,3) holds
    [InlineData("-1", false)]
    [InlineData("-0.001", false)]
    [InlineData("1.2345", false)]
    [InlineData("1000000000000000", false)]        // 16 integer digits do not fit decimal(18,3)
    public void Threshold_IsOptional_NeverNegative_AndFitsAStockQuantity(string? threshold, bool expectedValid)
    {
        var value = threshold is null ? (decimal?)null : decimal.Parse(threshold, CultureInfo.InvariantCulture);

        var putResult = _setValidator.Validate(new SetLowStockThresholdRequest(value));
        var openingResult = _openingValidator.Validate(
            new AddOpeningStockRequest(1, [new OpeningStockLine(ProductId: 1, Quantity: 5m, UnitCost: 10m, LowStockThreshold: value)]));

        Assert.Equal(expectedValid, putResult.IsValid);
        Assert.Equal(expectedValid, openingResult.IsValid);

        if (!expectedValid)
        {
            Assert.Contains(putResult.Errors, e => e.PropertyName == nameof(SetLowStockThresholdRequest.LowStockThreshold));
            Assert.Contains(openingResult.Errors, e => e.PropertyName == "Items[0].LowStockThreshold");
        }
    }
}
