namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to set the low-stock threshold of one product in one warehouse (DR-41); the warehouse and the product are
/// named in the route.
/// </summary>
/// <param name="LowStockThreshold">
/// The quantity at or below which the row counts as «Заканчивается» (≥ 0, at most 3 decimals, like stock);
/// <see langword="null"/> stops tracking the row.
/// </param>
public sealed record SetLowStockThresholdRequest(decimal? LowStockThreshold);
