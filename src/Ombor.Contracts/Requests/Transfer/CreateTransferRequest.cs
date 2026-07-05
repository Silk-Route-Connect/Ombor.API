namespace Ombor.Contracts.Requests.Transfer;

/// <summary>
/// Request to move stock from one warehouse to another. Atomic and immutable; each line is hard-blocked
/// over the source stock.
/// </summary>
/// <param name="FromWarehouseId">The source warehouse.</param>
/// <param name="ToWarehouseId">The destination warehouse.</param>
/// <param name="Note">Optional free-text note.</param>
/// <param name="Lines">The products and quantities to move.</param>
public sealed record CreateTransferRequest(
    int FromWarehouseId,
    int ToWarehouseId,
    string? Note,
    CreateTransferLine[] Lines);

/// <param name="ProductId">The product to move.</param>
/// <param name="Quantity">The quantity to move (must be &gt; 0).</param>
public sealed record CreateTransferLine(int ProductId, decimal Quantity);
