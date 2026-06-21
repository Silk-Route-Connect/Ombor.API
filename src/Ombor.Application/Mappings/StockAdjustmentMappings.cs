using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class StockAdjustmentMappings
{
    public static StockAdjustmentDto ToDto(this StockAdjustment adjustment)
    {
        if (adjustment.Warehouse is null || adjustment.Product is null)
        {
            throw new InvalidOperationException(
                "Cannot map StockAdjustment to StockAdjustmentDto because Warehouse or Product is null.");
        }

        return new(
            adjustment.Id,
            adjustment.DateUtc,
            adjustment.WarehouseId,
            adjustment.Warehouse.Name,
            adjustment.ProductId,
            adjustment.Product.Name,
            adjustment.Product.SKU,
            adjustment.Product.Category?.Name,
            adjustment.Product.Measurement.ToString(),
            adjustment.Direction.ToString(),
            adjustment.Quantity,
            adjustment.Reason,
            adjustment.Note,
            adjustment.CreatedBy);
    }
}
