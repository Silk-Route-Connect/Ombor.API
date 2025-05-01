using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class DeleteProductTests : ProductTestsBase
{
    private const int CategoryId = 100;

    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new DeleteProductRequest(CategoryId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<DeleteProductRequest>()), Times.Once);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new DeleteProductRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        var categoryToDelete = _builder.ProductBuilder
            .WithId(CategoryId)
            .BuildAndPopulate();
        var request = new DeleteProductRequest(CategoryId);

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockSet = SetupProducts([.. _defaultProducts, categoryToDelete]);
        mockSet.Setup(s => s.Remove(It.Is<Product>(e => e == categoryToDelete)));

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        mockSet.Verify(c => c.Remove(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
