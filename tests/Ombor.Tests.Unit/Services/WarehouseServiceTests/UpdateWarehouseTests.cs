using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;
using ValidationException = FluentValidation.ValidationException;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class UpdateWarehouseTests : WarehouseTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateInvalidUpdateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DataAnnotationsValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<DataAnnotationsValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateInvalidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Warehouse>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Warehouses, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenNameIsNotUnique()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        var request = WarehouseRequestFactory.GenerateValidUpdateRequest(warehouse.Id);

        // Another warehouse already owns the requested name.
        var conflicting = _builder.WarehouseBuilder
            .WithId(warehouse.Id + 1)
            .WithName(request.Name)
            .BuildAndPopulate();

        SetupWarehouses([.. _defaultWarehouses, warehouse, conflicting]);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        // Warehouses getter is hit twice: once to load the entity, once for the unique-name query.
        _mockContext.Verify(mock => mock.Warehouses, Times.Exactly(2));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedWarehouse_WhenWarehouseIsValid()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        var request = WarehouseRequestFactory.GenerateValidUpdateRequest(warehouse.Id);

        SetupWarehouses([.. _defaultWarehouses, warehouse]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        WarehouseAssertionHelper.AssertEquivalent(request, response);
        WarehouseAssertionHelper.AssertEquivalent(request, warehouse);
        WarehouseAssertionHelper.AssertEquivalent(warehouse, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Warehouses getter is hit three times: load the entity, the unique-name query, the IsDeletable check.
        _mockContext.Verify(mock => mock.Warehouses, Times.Exactly(3));

        VerifyNoOtherCalls();
    }
}
