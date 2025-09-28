using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public sealed class DeleteInventoryTests : InventoryTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldRemoveInventory_WhenInventoryExists()
    {
        // Arrange
        var inventory = CreateInventory();
        var request = new DeleteInventoryRequest(inventory.Id);

        var mockSet = SetupInventories([.. _defaultInventories, inventory]);
        mockSet.Setup(mock => mock.Remove(inventory));

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(mock => mock.Remove(inventory), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Exactly(2));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenInventoryDoesNotExist()
    {
        // Arrange
        var request = new DeleteInventoryRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Inventory>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new DeleteInventoryRequest(InventoryId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
