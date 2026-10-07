using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

/// <summary>
/// Gives a realistic few demo stock rows a low-stock threshold (DR-41) once the stock is final: most rows stay untracked,
/// like a shop that sets reorder levels only for what it restocks regularly. Per warehouse, every
/// <see cref="TrackEveryNthRow"/>-th row (by product) is tracked at about a fifth of its stock — not low — and the
/// emptiest row on hand plus one sold-out row are tracked above what they hold, so the demo shows a handful of
/// «Заканчивается» rows (one or two per warehouse), some at zero. Driven by the seeded stock, not by chance.
/// </summary>
internal static class LowStockThresholdSeeder
{
    private const int TrackEveryNthRow = 8;
    private const decimal HealthyThresholdShare = 0.2m;
    private const decimal ThresholdStep = 5m;
    private const decimal SoldOutThreshold = 10m;

    /// <summary>Seeds the thresholds of the current organization's stock rows, unless any row is tracked already.</summary>
    public static async Task SeedAsync(IApplicationDbContext context)
    {
        if (await context.WarehouseItems.AnyAsync(i => i.LowStockThreshold != null))
        {
            return;
        }

        var items = await context.WarehouseItems.ToListAsync();

        foreach (var warehouse in items.GroupBy(i => i.WarehouseId))
        {
            var rows = warehouse.OrderBy(i => i.ProductId).ToList();

            for (var index = 0; index < rows.Count; index += TrackEveryNthRow)
            {
                var healthy = RoundDown(rows[index].Quantity * HealthyThresholdShare);
                if (healthy > 0m)
                {
                    rows[index].LowStockThreshold = healthy;
                }
            }

            var emptiest = rows.Where(i => i.Quantity > 0m).MinBy(i => (i.Quantity, i.ProductId));
            if (emptiest is not null)
            {
                emptiest.LowStockThreshold = RoundDown(emptiest.Quantity) + ThresholdStep;
            }

            var soldOut = rows.FirstOrDefault(i => i.Quantity == 0m);
            if (soldOut is not null)
            {
                soldOut.LowStockThreshold = SoldOutThreshold;
            }
        }

        await context.SaveChangesAsync();
    }

    private static decimal RoundDown(decimal value) => Math.Floor(value / ThresholdStep) * ThresholdStep;
}
