using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public sealed class DeleteSupplierTests : SupplierTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        //Arrange
        var request = new DeleteSupplierRequest(SupplierId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        //Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers.Remove(It.IsAny<Partner>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundExceprion_WhenSupplierDoesNotExist()
    {
        // Arrange
        var request = new DeleteSupplierRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Partner>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSupplier_WhenSupplierExists()
    {
        // Arrange
        var supplierToDelete = CreateSupplier();
        var request = new DeleteSupplierRequest(supplierToDelete.Id);

        var mockSet = SetupSuppliers([.. _defaultSuppliers, supplierToDelete]);
        mockSet.Setup(mock => mock.Remove(supplierToDelete));

        // Act
        await _service.DeleteAsync(request);

        // Then
        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(mock => mock.Remove(supplierToDelete), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers, Times.Exactly(2));

        VerifyNoOtherCalls();
    }

}
