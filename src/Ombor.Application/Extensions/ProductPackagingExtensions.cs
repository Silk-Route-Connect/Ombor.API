using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Extensions;

internal static class ProductPackagingExtensions
{
    /// <summary>
    /// Loads the configured package size (base units per package) for each of the given products, so a
    /// package-entry line can be resolved to its base-unit quantity server-side (rule 21). Returns an empty
    /// map when there are no product ids.
    /// </summary>
    public static async Task<IReadOnlyDictionary<int, int>> LoadPackageSizesAsync(
        this IApplicationDbContext context, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, int>();
        }

        return await context.Products
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Packaging.Size })
            .ToDictionaryAsync(x => x.Id, x => x.Size);
    }
}
