namespace Ombor.Domain.Entities;

public sealed class ProductPackaging
{
    public int Size { get; init; }
    public string? Label { get; init; }
    public string? Barcode { get; init; }
}
