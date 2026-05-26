namespace Ombor.Contracts.Requests.Transfer;

/// <summary>
/// Request to move stock from one warehouse to another.
/// </summary>
/// <param name="FromInventoryId">The source warehouse.</param>
/// <param name="ToInventoryId">The destination warehouse.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="Lines">The products and quantities to move.</param>
public sealed record CreateTransferRequest(
    int FromInventoryId,
    int ToInventoryId,
    string? Notes,
    CreateTransferLine[] Lines);

/// <param name="ProductId">The product to move.</param>
/// <param name="Quantity">The quantity to move (must be &gt; 0).</param>
public sealed record CreateTransferLine(int ProductId, int Quantity);
