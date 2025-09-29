using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public sealed class GetInventoriesByIdTests : InventoryTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenInventoryIsFound()
    {
        // Arrange
        var inventory = CreateInventory();
        var request = new GetInventoryByIdRequest(inventory.Id);

        SetupInventories([.. _defaultInventories, inventory]);

        // Act
        var response = await _service.GetByIdAsync(request);

        // Assert
        InventoryAssertionHelper.AssertEquivalent(inventory, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenInventoryDoesNotExist()
    {
        // Arrange
        var request = new GetInventoryByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Inventory>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidatoinException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetInventoryByIdRequest(InventoryId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
