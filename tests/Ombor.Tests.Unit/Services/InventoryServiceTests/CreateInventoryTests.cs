using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public sealed class CreateInventoryTests : InventoryTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation error"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedInventory_WhenRequestIsValid()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateValidCreateRequest();
        Inventory addedInventory = null!;

        _mockContext.Setup(mock => mock.Inventories.Add(It.Is<Inventory>(inventory => inventory.IsEquivalent(request))))
            .Callback<Inventory>(captured => addedInventory = captured);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => addedInventory.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        InventoryAssertionHelper.AssertEquivalent(request, response);
        InventoryAssertionHelper.AssertEquivalent(request, addedInventory);
        InventoryAssertionHelper.AssertEquivalent(addedInventory, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Inventories.Add(addedInventory), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
