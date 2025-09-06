namespace Ombor.Contracts.Responses.Product;

public sealed record ProductPackagingDto(
    int Size,
    string? Label,
    string? Barcode);
