using Moq;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
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

        _mockMapping.Setup(mock => mock.ToEntity(It.IsAny<CreateInventoryRequest>()))
            .Returns(new Inventory
            {
                Name = request.Name,
                IsActive = request.IsActive,
                Location = request.Location
            });

        // Act
        var response = _mockMapping.Object.ToEntity(request);

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

        _mockMapping.Setup(mock => mock.ToCreateResponse(It.IsAny<Inventory>()))
            .Returns(new CreateInventoryResponse(
                request.Id,
                request.Name,
                request.Location,
                request.IsActive));

        // Act
        var response = _mockMapping.Object.ToCreateResponse(request);

        // Assert
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

        _mockMapping.Setup(mock => mock.ToUpdateResponse(It.IsAny<Inventory>()))
            .Returns(new UpdateInventoryResponse(
                request.Id,
                request.Name,
                request.Location,
                request.IsActive));

        // Act
        var response = _mockMapping.Object.ToUpdateResponse(request);

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

        _mockMapping.Setup(mock => mock.ToDto(It.IsAny<Inventory>()))
            .Returns(new InventoryDto(
                request.Id,
                request.Name,
                request.Location,
                request.IsActive));

        // Act
        var response = _mockMapping.Object.ToDto(request);

        // Assert
        Assert.Equal(request.Id, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
        Assert.Equal(request.IsActive, response.IsActive);
    }
}
