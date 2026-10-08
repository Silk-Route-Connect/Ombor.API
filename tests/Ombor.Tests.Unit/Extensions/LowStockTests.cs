using System.Globalization;
using Ombor.Application.Extensions;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Extensions;

public sealed class LowStockTests
{
    [Theory]
    // Tracked: at or below the threshold is low, above it is not; zero counts.
    [InlineData("5", "10", false, false, true)]
    [InlineData("10", "10", false, false, true)]
    [InlineData("0", "3", false, false, true)]
    [InlineData("0", "0", false, false, true)]
    [InlineData("10.001", "10", false, false, false)]
    [InlineData("20", "10", false, false, false)]
    // Not tracked: never low, not even at zero.
    [InlineData("0", null, false, false, false)]
    [InlineData("1", null, false, false, false)]
    // An archived product or warehouse never counts.
    [InlineData("2", "10", true, false, false)]
    [InlineData("2", "10", false, true, false)]
    public void IsLowStock_AndTheQueryPredicate_ApplyTheSameRule(
        string quantity, string? threshold, bool productIsArchived, bool warehouseIsArchived, bool expected)
    {
        var item = new WarehouseItem
        {
            Quantity = decimal.Parse(quantity, CultureInfo.InvariantCulture),
            LowStockThreshold = threshold is null ? null : decimal.Parse(threshold, CultureInfo.InvariantCulture),
            Product = new Product { Name = "P", SKU = "P", IsArchived = productIsArchived, Category = null! },
            Warehouse = new Warehouse { Name = "W", IsArchived = warehouseIsArchived },
        };

        Assert.Equal(expected, LowStock.IsLowStock(item.Quantity, item.LowStockThreshold, productIsArchived, warehouseIsArchived));
        Assert.Equal(expected, LowStock.IsLow.Compile()(item));
    }
}
