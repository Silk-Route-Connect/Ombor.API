using Moq;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;
using ValidationException = FluentValidation.ValidationException;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class CreateWarehouseTests : WarehouseTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DataAnnotationsValidationException("Validation error"));

        // Act & Assert
        await Assert.ThrowsAsync<DataAnnotationsValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenNameIsNotUnique()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateValidCreateRequest();
        var existing = _builder.WarehouseBuilder
            .WithId(500)
            .WithName(request.Name)
            .BuildAndPopulate();

        SetupWarehouses([.. _defaultWarehouses, existing]);

        // Act & Assert — the unique-name guard rejects the duplicate (→ 400).
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Warehouses, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedWarehouse_WhenRequestIsValid()
    {
        // Arrange — empty set so the name is unique; stub Add on the DbSet mock itself
        // so the unique-name query (AnyAsync) still runs against a real queryable provider.
        var request = WarehouseRequestFactory.GenerateValidCreateRequest();
        var mockSet = SetupWarehouses([]);
        Warehouse addedWarehouse = null!;

        mockSet.Setup(mock => mock.Add(It.Is<Warehouse>(warehouse => warehouse.IsEquivalent(request))))
            .Callback<Warehouse>(captured => addedWarehouse = captured);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => addedWarehouse.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        WarehouseAssertionHelper.AssertEquivalent(request, response);
        WarehouseAssertionHelper.AssertEquivalent(request, addedWarehouse);
        WarehouseAssertionHelper.AssertEquivalent(addedWarehouse, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        // Warehouses getter is hit twice: once for the unique-name query, once for Add.
        _mockContext.Verify(mock => mock.Warehouses, Times.Exactly(2));
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
