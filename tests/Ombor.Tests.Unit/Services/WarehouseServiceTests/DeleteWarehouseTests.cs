using FluentValidation;
using Moq;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class DeleteWarehouseTests : WarehouseTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new DeleteWarehouseRequest(WarehouseId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var request = new DeleteWarehouseRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Warehouse>>(
            () => _service.DeleteAsync(request));

        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHardDeleteWarehouse_WhenNotReferenced()
    {
        // Arrange — an empty warehouse with no stock, movements, transfers, transactions, or orders.
        var warehouse = _builder.WarehouseBuilder
            .WithId(800)
            .WithWarehouseItems([])
            .Build();
        var request = new DeleteWarehouseRequest(warehouse.Id);

        var mockSet = SetupWarehouses([.. _defaultWarehouses, warehouse]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Assert — an unreferenced warehouse is hard-deleted.
        mockSet.Verify(mock => mock.Remove(It.Is<Warehouse>(e => e.Id == warehouse.Id)), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowConflict_WhenWarehouseHasStock()
    {
        // Arrange — a warehouse that still holds stock items.
        var warehouse = _builder.WarehouseBuilder
            .WithId(801)
            .BuildAndPopulate();
        var request = new DeleteWarehouseRequest(warehouse.Id);

        var mockSet = SetupWarehouses([.. _defaultWarehouses, warehouse]);

        // Act & Assert — a referenced warehouse cannot be hard-deleted (409).
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(request));

        mockSet.Verify(mock => mock.Remove(It.IsAny<Warehouse>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowConflict_WhenWarehouseReferencedByTransaction()
    {
        // Arrange — no stock, but a transaction references the warehouse.
        var warehouse = _builder.WarehouseBuilder
            .WithId(802)
            .WithWarehouseItems([])
            .Build();
        var request = new DeleteWarehouseRequest(warehouse.Id);

        var mockSet = SetupWarehouses([.. _defaultWarehouses, warehouse]);
        SetupTransactions([new TransactionRecord { Id = 1, WarehouseId = warehouse.Id, Partner = null! }]);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(request));

        mockSet.Verify(mock => mock.Remove(It.IsAny<Warehouse>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
