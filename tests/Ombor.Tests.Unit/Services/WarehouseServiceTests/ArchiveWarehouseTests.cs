using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class ArchiveWarehouseTests : WarehouseTestsBase
{
    [Fact]
    public async Task ArchiveAsync_ShouldSetIsArchivedTrue_AndReturnDto()
    {
        // Arrange
        var warehouse = _builder.WarehouseBuilder
            .WithId(700)
            .WithIsArchived(false)
            .BuildAndPopulate();

        SetupWarehouses([.. _defaultWarehouses, warehouse]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var response = await _service.ArchiveAsync(warehouse.Id);

        // Assert
        Assert.True(warehouse.IsArchived);
        Assert.True(response.IsArchived);
        Assert.Equal(warehouse.Id, response.Id);

        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldThrowNotFound_WhenWarehouseDoesNotExist()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Warehouse>>(
            () => _service.ArchiveAsync(NonExistentEntityId));
    }

    [Fact]
    public async Task RestoreAsync_ShouldSetIsArchivedFalse_AndReturnDto()
    {
        // Arrange
        var warehouse = _builder.WarehouseBuilder
            .WithId(701)
            .WithIsArchived(true)
            .BuildAndPopulate();

        SetupWarehouses([.. _defaultWarehouses, warehouse]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var response = await _service.RestoreAsync(warehouse.Id);

        // Assert
        Assert.False(warehouse.IsArchived);
        Assert.False(response.IsArchived);
        Assert.Equal(warehouse.Id, response.Id);

        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ShouldThrowNotFound_WhenWarehouseDoesNotExist()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Warehouse>>(
            () => _service.RestoreAsync(NonExistentEntityId));
    }
}
