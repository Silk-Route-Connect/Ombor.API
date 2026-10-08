using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Unit.Services.WarehouseServiceTests;

namespace Ombor.Tests.Unit.Mappings;

public class WarehouseMappingsTests : WarehouseTestsBase
{
    [Fact]
    public void ToEntity_ShouldMapNameAndLocation_AndDefaultToNotArchived()
    {
        // Arrange
        var request = new CreateWarehouseRequest("Test name", "Test location");

        // Act
        var response = request.ToEntity();

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.False(response.IsArchived);
        Assert.Empty(response.WarehouseItems);
    }

    [Fact]
    public void ToDto_ShouldComputeTotals_OverWarehouseItems()
    {
        // Arrange — quantities and costs chosen so totals are unambiguous.
        var warehouse = new Warehouse
        {
            Id = 3,
            Name = "test name3",
            Location = "test location3",
            IsArchived = true,
            WarehouseItems =
            [
                new WarehouseItem { ProductId = 1, Quantity = 10, AverageCost = 100m, Warehouse = null!, Product = null! },
                new WarehouseItem { ProductId = 2, Quantity = 5, AverageCost = 200m, Warehouse = null!, Product = null! },
                new WarehouseItem { ProductId = 3, Quantity = 2, AverageCost = 50m, Warehouse = null!, Product = null! },
                new WarehouseItem { ProductId = 4, Quantity = 0, AverageCost = 70m, Warehouse = null!, Product = null! },
            ]
        };

        // Act
        var response = warehouse.ToDto(isDeletable: false, lowStockCount: 2);

        // Assert
        Assert.Equal(warehouse.Id, response.Id);
        Assert.Equal(warehouse.Name, response.Name);
        Assert.Equal(warehouse.Location, response.Location);
        Assert.True(response.IsArchived);
        Assert.Equal(3, response.ProductCount); // the emptied row is not on hand
        Assert.Equal(2, response.LowStockCount);
        Assert.Equal(2_100m, response.StockValue); // 10*100 + 5*200 + 2*50
        Assert.False(response.IsDeletable);
    }

    [Fact]
    public void ToDto_ShouldReturnZeroTotals_WhenNoItems()
    {
        // Arrange
        var warehouse = new Warehouse
        {
            Id = 4,
            Name = "empty",
            Location = null,
            IsArchived = false,
            WarehouseItems = []
        };

        // Act
        var response = warehouse.ToDto(isDeletable: true, lowStockCount: 0);

        // Assert
        Assert.Equal(0, response.ProductCount);
        Assert.Equal(0, response.LowStockCount);
        Assert.Equal(0m, response.StockValue);
        Assert.False(response.IsArchived);
        Assert.True(response.IsDeletable);
    }

    [Fact]
    public void ToStockItemDto_ShouldMapProductDetails_AndComputeValue()
    {
        // Arrange
        var category = new Category { Name = "Drinks" };
        var product = new Product
        {
            Id = 7,
            Name = "Cola",
            SKU = "SKU-7",
            Measurement = UnitOfMeasurement.Piece,
            Category = category,
        };
        var item = new WarehouseItem
        {
            ProductId = product.Id,
            Quantity = 12,
            AverageCost = 25m,
            Product = product,
            Warehouse = null!,
        };

        // Act
        var response = item.ToStockItemDto(warehouseIsArchived: false);

        // Assert
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal(product.Name, response.ProductName);
        Assert.Equal(product.SKU, response.Sku);
        Assert.Equal(category.Name, response.CategoryName);
        Assert.Equal(product.Measurement.ToString(), response.Measurement);
        Assert.Equal(item.Quantity, response.Quantity);
        Assert.Equal(item.AverageCost, response.AverageCost);
        Assert.Equal(300m, response.Value); // 12 * 25
        Assert.Null(response.LowStockThreshold);
        Assert.False(response.IsLowStock);
    }

    [Theory]
    [InlineData(5, false, false, true)]
    [InlineData(4, false, false, false)]
    [InlineData(5, true, false, false)]
    [InlineData(5, false, true, false)]
    public void ToStockItemDto_ShouldFlagLowStock_OnlyForTrackedActiveRowsAtOrBelowTheThreshold(
        int threshold, bool productIsArchived, bool warehouseIsArchived, bool expected)
    {
        // Arrange — 5 on hand.
        var product = new Product
        {
            Id = 10,
            Name = "Tea",
            SKU = "SKU-10",
            IsArchived = productIsArchived,
            Measurement = UnitOfMeasurement.Piece,
            Category = new Category { Name = "Drinks" },
        };
        var item = new WarehouseItem
        {
            ProductId = product.Id,
            Quantity = 5,
            AverageCost = 10m,
            LowStockThreshold = threshold,
            Product = product,
            Warehouse = null!,
        };

        // Act
        var response = item.ToStockItemDto(warehouseIsArchived);

        // Assert
        Assert.Equal((decimal?)threshold, response.LowStockThreshold);
        Assert.Equal(expected, response.IsLowStock);
    }

    [Fact]
    public void ToStockItemDto_ShouldMapNullCategory_WhenProductHasNoCategory()
    {
        // Arrange
        var product = new Product
        {
            Id = 8,
            Name = "Uncategorized",
            SKU = "SKU-8",
            Measurement = UnitOfMeasurement.Kilogram,
            Category = null!,
        };
        var item = new WarehouseItem
        {
            ProductId = product.Id,
            Quantity = 3,
            AverageCost = 10m,
            Product = product,
            Warehouse = null!,
        };

        // Act
        var response = item.ToStockItemDto(warehouseIsArchived: false);

        // Assert
        Assert.Null(response.CategoryName);
        Assert.Equal(30m, response.Value);
    }

    [Fact]
    public void ToStockItemDto_ShouldThrow_WhenProductIsNull()
    {
        // Arrange
        var item = new WarehouseItem
        {
            ProductId = 9,
            Quantity = 1,
            AverageCost = 1m,
            Product = null!,
            Warehouse = null!,
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => item.ToStockItemDto(warehouseIsArchived: false));
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteNameAndLocation()
    {
        // Arrange
        var warehouse = new Warehouse
        {
            Id = 21,
            Name = "Test warehouse",
            Location = "Test location",
            IsArchived = true,
        };

        var request = new UpdateWarehouseRequest(
            Id: warehouse.Id,
            Name: "Updated warehouse",
            Location: "Updated test location");

        // Act
        warehouse.ApplyUpdate(request);

        // Assert
        Assert.Equal(request.Name, warehouse.Name);
        Assert.Equal(request.Location, warehouse.Location);
        // Archive state is managed via archive/restore, not ApplyUpdate.
        Assert.True(warehouse.IsArchived);
    }
}
