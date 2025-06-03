using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public sealed class GetSupplierByIdTests : SupplierTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetSupplierByIdRequest(SupplierId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenSupplierDoesNotExist()
    {
        // Arrange 
        var request = new GetSupplierByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Supplier>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenSupplierIsFound()
    {
        // Arrange
        var expected = CreateSupplier();
        var request = new GetSupplierByIdRequest(expected.Id);

        SetupSuppliers([.. _defaultSuppliers, expected]);

        // Act 
        var response = await _service.GetByIdAsync(request);

        // Assert
        SupplierAssertionHelper.AssertEquivalent(expected, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }
}
