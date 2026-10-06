namespace Ombor.Contracts.Requests.StockAdjustment;

/// <summary>Request to read one stock adjustment.</summary>
/// <param name="Id">The adjustment id.</param>
public sealed record GetStockAdjustmentByIdRequest(int Id);
