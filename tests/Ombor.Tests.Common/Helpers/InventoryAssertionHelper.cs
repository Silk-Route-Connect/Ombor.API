using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class InventoryAssertionHelper
{
    public static void AssertEquivalent(Inventory? expected, InventoryDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(CreateInventoryRequest? expected, CreateInventoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(CreateInventoryRequest? expected, Inventory? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(Inventory? expected, CreateInventoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(UpdateInventoryRequest? expected, UpdateInventoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(UpdateInventoryRequest? expected, Inventory? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(Inventory? expected, UpdateInventoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }
}
