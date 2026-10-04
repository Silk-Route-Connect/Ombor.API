using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>A report row before its figures: the key, the label and the lines it covers.</summary>
internal sealed record ReportGroup(string Key, string Label, ReportLine[] Lines);

/// <summary>
/// Splits document lines into report rows. A time grouping yields every bucket of the period in order, empty ones
/// included, so a chart has no gaps; an entity grouping yields the entities with lines, labelled by their current name
/// (archived ones too) — a product's category is its current one.
/// </summary>
internal sealed class ReportGrouping(IApplicationDbContext context)
{
    public async Task<ReportGroup[]> GroupAsync(ReportLine[] lines, ReportGroupBy groupBy, ReportRange range)
    {
        if (ReportBuckets.IsTime(groupBy))
        {
            var byKey = lines.ToLookup(l => ReportBuckets.KeyOf(groupBy, l.Date));

            return [.. ReportBuckets.KeysOf(groupBy, range).Select(key => new ReportGroup(key, key, [.. byKey[key]]))];
        }

        var groups = lines.GroupBy(l => EntityOf(groupBy, l)).ToArray();
        var names = await NamesAsync(groupBy, [.. groups.Select(g => g.Key)]);

        return [.. groups.Select(g => new ReportGroup(
            g.Key.ToString(CultureInfo.InvariantCulture),
            names.GetValueOrDefault(g.Key, string.Empty),
            [.. g]))];
    }

    private static int EntityOf(ReportGroupBy groupBy, ReportLine line) => groupBy switch
    {
        ReportGroupBy.Product => line.ProductId,
        ReportGroupBy.Category => line.CategoryId,
        ReportGroupBy.Partner => line.PartnerId,
        ReportGroupBy.Warehouse => line.WarehouseId,
        _ => throw new ArgumentOutOfRangeException(nameof(groupBy), groupBy, "Not an entity grouping."),
    };

    private Task<Dictionary<int, string>> NamesAsync(ReportGroupBy groupBy, int[] ids) => groupBy switch
    {
        ReportGroupBy.Product => context.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name),
        ReportGroupBy.Category => context.Categories.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name),
        ReportGroupBy.Partner => context.Partners.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name),
        _ => context.Warehouses.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name),
    };
}
