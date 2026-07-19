using FluentValidation;

namespace Ombor.Application.Mappings;

/// <summary>
/// Resolves a transaction line / template item entered in packages into its base-unit quantity and the
/// package-size snapshot (rule 21). The package size is authoritative on the product, never supplied by the
/// client, so the base quantity is computed server-side: <c>pack count × size</c>. A base-unit entry (no pack
/// count) passes its quantity through unchanged with a null snapshot.
/// </summary>
internal static class PackageEntry
{
    /// <param name="productId">The line's product.</param>
    /// <param name="requestQuantity">The base-unit quantity from the request (used only for a base-unit entry).</param>
    /// <param name="packageQuantity">The entered pack count, or null for a base-unit entry.</param>
    /// <param name="packageSizes">Product id → configured package size, for the package-entry products.</param>
    /// <returns>The base-unit quantity to persist and the package-size snapshot (null for a base-unit entry).</returns>
    public static (decimal Quantity, int? PackageSize) Resolve(
        int productId,
        decimal requestQuantity,
        int? packageQuantity,
        IReadOnlyDictionary<int, int> packageSizes)
    {
        if (packageQuantity is not int count || count <= 0)
        {
            return (requestQuantity, null);
        }

        if (!packageSizes.TryGetValue(productId, out var size) || size <= 0)
        {
            throw new ValidationException(
                $"Product {productId} has no configured package size and cannot be entered in packages.");
        }

        return (count * size, size);
    }
}
