namespace Ombor.Contracts.Requests.StockAdjustment;

/// <summary>Filters for the stock-adjustments list.</summary>
/// <param name="WarehouseId">Optional warehouse to filter by.</param>
/// <param name="ProductId">Optional product to filter by.</param>
public sealed record GetStockAdjustmentsRequest(
    int? WarehouseId,
    int? ProductId);
