namespace Ombor.Contracts.Responses.Transfer;

/// <summary>An immutable inter-warehouse stock transfer.</summary>
/// <param name="Id">The transfer id.</param>
/// <param name="Date">When the transfer happened.</param>
/// <param name="FromWarehouseId">The source warehouse.</param>
/// <param name="FromWarehouseName">The source warehouse name.</param>
/// <param name="ToWarehouseId">The destination warehouse.</param>
/// <param name="ToWarehouseName">The destination warehouse name.</param>
/// <param name="Note">Optional free-text note.</param>
/// <param name="CreatedBy">The user who made the transfer, if known.</param>
/// <param name="Lines">The product lines moved.</param>
public sealed record TransferDto(
    int Id,
    DateTimeOffset Date,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    string? Note,
    string? CreatedBy,
    TransferLineDto[] Lines);

/// <summary>A single product line of a transfer.</summary>
/// <param name="Id">The line id.</param>
/// <param name="ProductId">The product moved.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Quantity">The quantity moved.</param>
public sealed record TransferLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    string Measurement,
    int Quantity);
