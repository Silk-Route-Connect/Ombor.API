namespace Ombor.Contracts.Requests.Transfer;

/// <summary>
/// Request to list transfers, optionally filtered to a single warehouse (as source or destination).
/// </summary>
public sealed record GetTransfersRequest(int? WarehouseId = null);
