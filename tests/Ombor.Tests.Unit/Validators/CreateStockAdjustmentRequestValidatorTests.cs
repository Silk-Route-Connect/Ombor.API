using Ombor.Application.Validators.StockAdjustment;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.StockAdjustment;

namespace Ombor.Tests.Unit.Validators;

public sealed class CreateStockAdjustmentRequestValidatorTests
{
    private readonly CreateStockAdjustmentRequestValidator _validator = new();

    [Theory]
    // Decrease reasons.
    [InlineData(StockAdjustmentDirection.Decrease, "Damage", true)]
    [InlineData(StockAdjustmentDirection.Decrease, "RecountDown", true)]
    [InlineData(StockAdjustmentDirection.Decrease, "Found", false)]   // Found is increase-only
    // Increase reasons.
    [InlineData(StockAdjustmentDirection.Increase, "Found", true)]
    [InlineData(StockAdjustmentDirection.Increase, "RecountUp", true)]
    [InlineData(StockAdjustmentDirection.Increase, "Damage", false)]  // Damage is decrease-only
    // Other is valid for both.
    [InlineData(StockAdjustmentDirection.Increase, "Other", true)]
    [InlineData(StockAdjustmentDirection.Decrease, "Other", true)]
    [InlineData(StockAdjustmentDirection.Decrease, "Nonsense", false)]
    public void Reason_IsValidatedPerDirection(StockAdjustmentDirection direction, string reason, bool expectedValid)
    {
        var request = new CreateStockAdjustmentRequest(WarehouseId: 1, ProductId: 1, direction, Quantity: 5, reason, Note: null);

        var result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0, 1, 5)]
    [InlineData(1, 0, 5)]
    [InlineData(1, 1, 0)]
    public void Rejects_NonPositiveIds_AndQuantity(int warehouseId, int productId, int quantity)
    {
        var request = new CreateStockAdjustmentRequest(warehouseId, productId, StockAdjustmentDirection.Increase, quantity, "Found", null);

        Assert.False(_validator.Validate(request).IsValid);
    }
}
