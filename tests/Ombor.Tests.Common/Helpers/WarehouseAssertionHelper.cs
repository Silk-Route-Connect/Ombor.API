using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class WarehouseAssertionHelper
{
    public static void AssertEquivalent(Warehouse? expected, WarehouseDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsArchived, actual.IsArchived);

        // Totals are computed from the warehouse's items (rule 12 — server-computed, never stored).
        var items = expected.WarehouseItems;
        Assert.Equal(items.Count, actual.ProductCount);
        Assert.Equal(items.Sum(i => i.Quantity), actual.TotalUnits);
        Assert.Equal(items.Sum(i => i.Quantity * i.AverageCost), actual.StockValue);
    }

    public static void AssertEquivalent(CreateWarehouseRequest? expected, WarehouseDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
    }

    public static void AssertEquivalent(CreateWarehouseRequest? expected, Warehouse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
    }

    public static void AssertEquivalent(UpdateWarehouseRequest? expected, WarehouseDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
    }

    public static void AssertEquivalent(UpdateWarehouseRequest? expected, Warehouse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
    }
}
