using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public sealed class UpdateSupplierTests : SupplierTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange 
        var request = SupplierRequestFactory.GenerateInalidUpdateRequest(SupplierId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
             () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNorFoundException_WhenSupplierDoesNotExist()
    {
        // Arrange
        var request = SupplierRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Supplier>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedSupplier_WhenRequestIsValid()
    {
        // Arrange
        var supplierToUpdate = CreateSupplier();
        var request = SupplierRequestFactory.GenerateValidUpdateRequest(supplierToUpdate.Id);

        SetupSuppliers([.. _defaultSuppliers, supplierToUpdate]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        SupplierAssertionHelper.AssertEquivalent(request, response);
        SupplierAssertionHelper.AssertEquivalent(request, supplierToUpdate);
        SupplierAssertionHelper.AssertEquivalent(supplierToUpdate, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }
}
