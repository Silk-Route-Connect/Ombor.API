using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.TestDataGenerator.Generators.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class DeleteProductTests : ProductTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Never);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new DeleteProductRequest(NonExistentEntityId);

        _mockContext.Setup(c => c.Products.Remove(It.IsAny<Product>()));
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        SetupProducts(_defaultProducts);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.DeleteAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        var toDelete = ProductGenerator.Generate([1, 2]);

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockSet = SetupProducts([.. _defaultProducts, toDelete]);
        mockSet.Setup(s => s.Remove(It.IsAny<Product>()));

        var request = new DeleteProductRequest(toDelete.Id);

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        mockSet.Verify(c => c.Remove(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
