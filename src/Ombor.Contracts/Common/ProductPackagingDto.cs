namespace Ombor.Contracts.Common;

/// <summary>
/// Represents the packaging details of a product, including size, label, and barcode.
/// </summary>
/// <param name="Size">The size of the product packaging, represented as an integer. Must be a positive value.</param>
/// <param name="Label">The label associated with the product packaging. Can be <see langword="null"/> if no label is provided.</param>
/// <param name="Barcode">The barcode of the product packaging. Can be <see langword="null"/> if no barcode is assigned.</param>
public sealed record ProductPackagingDto(
    int Size,
    string? Label,
    string? Barcode);
