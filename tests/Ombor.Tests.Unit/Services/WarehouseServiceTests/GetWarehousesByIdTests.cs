using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class GetWarehousesByIdTests : WarehouseTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenWarehouseIsFound()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        var request = new GetWarehouseByIdRequest(warehouse.Id);

        SetupWarehouses([.. _defaultWarehouses, warehouse]);

        // Act
        var response = await _service.GetByIdAsync(request);

        // Assert
        WarehouseAssertionHelper.AssertEquivalent(warehouse, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        // Warehouses is hit twice: once to load the entity, once for the IsDeletable reference check.
        _mockContext.Verify(mock => mock.Warehouses, Times.Exactly(2));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var request = new GetWarehouseByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Warehouse>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Warehouses, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidatoinException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetWarehouseByIdRequest(WarehouseId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
