namespace Ombor.Contracts.Responses.Transfer;

/// <summary>
/// DTO representing an inter-warehouse stock transfer.
/// </summary>
public sealed record TransferDto(
    int Id,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    DateTimeOffset DateUtc,
    string Status,
    string? Notes,
    TransferLineDto[] Lines);

/// <summary>
/// DTO representing a single product line of a <see cref="TransferDto"/>.
/// </summary>
public sealed record TransferLineDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity);
