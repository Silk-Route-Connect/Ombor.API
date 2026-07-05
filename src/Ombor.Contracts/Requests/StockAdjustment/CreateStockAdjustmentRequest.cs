using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.StockAdjustment;

/// <summary>Records a stock adjustment (loss or correction) for one product in one warehouse.</summary>
/// <param name="WarehouseId">The warehouse being adjusted.</param>
/// <param name="ProductId">The product being adjusted.</param>
/// <param name="Direction">Increase or Decrease.</param>
/// <param name="Quantity">The adjusted quantity (positive magnitude).</param>
/// <param name="Reason">A reason from the set allowed for the direction.</param>
/// <param name="Note">Optional free-text note.</param>
public sealed record CreateStockAdjustmentRequest(
    int WarehouseId,
    int ProductId,
    StockAdjustmentDirection Direction,
    decimal Quantity,
    string Reason,
    string? Note);
