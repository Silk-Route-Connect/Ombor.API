using System.Data.Common;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Unit.Services.InventoryServiceTests;

namespace Ombor.Tests.Unit.Mappings;

public class InventoryMappingsTests : InventoryTestsBase
{

    [Fact]
    public void ToEntity_ShouldMapAllFields()
    {
        // Arrange
        var request = new CreateInventoryRequest("Test name", "Test location", true);

        // Act
        var response = request.ToEntity();

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.Equal(request.IsActive, response.IsActive);
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFields()
    {
        // Arrange
        var request = new Inventory
        {
            Id = 1,
            Name = "test name",
            Location = "test location",
            IsActive = true
        };

        // Act 
        var response = request.ToCreateResponse();

        // & Assert
        Assert.Equal(request.Id, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.Equal(request.IsActive, response.IsActive);
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFields()
    {
        // Arrange
        var request = new Inventory
        {
            Id = 2,
            Name = "test name2",
            Location = "test location2",
            IsActive = false
        };

        // Act 
        var response = request.ToUpdateResponse();

        // Assert
        Assert.Equal(request.Id, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.Equal(request.IsActive, response.IsActive);
    }

    [Fact]
    public void ToDto_ShouldMapAllFields()
    {
        // Arrange
        var request = new Inventory
        {
            Id = 3,
            Name = "test name3",
            Location = "test location3",
            IsActive = true
        };

        // Act
        var response = request.ToDto();

        // Assert
        Assert.Equal(request.Id, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.Equal(request.IsActive, response.IsActive);
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteAllFields()
    {
        // Arrange
        var inventory = new Inventory
        {
            Id = 21,
            Name = "Test inventory",
            Location = "Test location",
            IsActive = true
        };

        var request = new UpdateInventoryRequest(
            Id: inventory.Id,
            Name: "Updated inventory",
            Location: "Updated test location",
            IsActive: false);

        // Act
        inventory.ApplyUpdate(request);

        // Assert
        Assert.Equal(request.Id, inventory.Id);
        Assert.Equal(request.Name, inventory.Name);
        Assert.Equal(request.Location, inventory.Location);
        Assert.Equal(request.IsActive, inventory.IsActive);
    }
}
