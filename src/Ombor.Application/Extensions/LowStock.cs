using System.Linq.Expressions;
using Ombor.Domain.Entities;

namespace Ombor.Application.Extensions;

/// <summary>
/// The one low-stock rule (DR-41), shared by the warehouse stock tab, the warehouse list, the stock report and the
/// bell: a warehouse item is low when it has a threshold and its quantity is at or below it. An item without a
/// threshold is not tracked, whatever its quantity — a catalogue product never stocked is not «заканчивается».
/// </summary>
/// <remarks>
/// An archived product or warehouse never counts: nobody restocks it, so it would sit in every alert for good. The
/// per-row flag follows the same rule as the counts, so a client filtering rows by it lists exactly what a count says.
/// </remarks>
internal static class LowStock
{
    /// <summary>The rule as a query predicate, for counting in SQL.</summary>
    public static readonly Expression<Func<WarehouseItem, bool>> IsLow = item =>
        item.LowStockThreshold != null
        && item.Quantity <= item.LowStockThreshold
        && !item.Product.IsArchived
        && !item.Warehouse.IsArchived;

    /// <summary>The rule over values already loaded; agrees with <see cref="IsLow"/>.</summary>
    public static bool IsLowStock(decimal quantity, decimal? threshold, bool productIsArchived, bool warehouseIsArchived) =>
        threshold is decimal limit
        && quantity <= limit
        && !productIsArchived
        && !warehouseIsArchived;
}
