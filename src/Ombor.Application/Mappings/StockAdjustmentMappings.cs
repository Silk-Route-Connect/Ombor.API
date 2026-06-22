using Ombor.Application.Extensions;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class StockAdjustmentMappings
{
    /// <param name="balanceAfter">
    /// The product's stock in this warehouse right after the adjustment — a movement-ledger figure
    /// the caller derives (it isn't stored on the event).
    /// </param>
    public static StockAdjustmentDto ToDto(this StockAdjustment adjustment, int balanceAfter)
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
            adjustment.CreatedByUser.DisplayName(),
            balanceAfter);
    }
}
