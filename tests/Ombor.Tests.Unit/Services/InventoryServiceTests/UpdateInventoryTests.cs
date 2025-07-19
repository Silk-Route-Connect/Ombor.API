using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public sealed class UpdateInventoryTests : InventoryTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateInvalidUpdateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenInventoryDoesNotExist()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateInvalidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Inventory>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedInventory_WhenInventoryIsValid()
    {
        // Arrange
        var inventory = CreateInventory();
        var request = InventoryRequestFactory.GenerateValidUpdateRequest(inventory.Id);

        SetupInventories([.. _defaultInventories, inventory]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        InventoryAssertionHelper.AssertEquivalent(request, response);
        InventoryAssertionHelper.AssertEquivalent(request, inventory);
        InventoryAssertionHelper.AssertEquivalent(inventory, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }
}
