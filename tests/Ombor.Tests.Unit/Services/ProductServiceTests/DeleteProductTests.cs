using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class DeleteProductTests : ProductTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new DeleteProductRequest(ProductId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new DeleteProductRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockContext.Verify(mock => mock.Products, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldHardDeleteProduct_WhenNotReferenced()
    {
        // Arrange
        var productToDelete = _builder.ProductBuilder
            .WithId(999)
            .WithImages([])
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        var request = new DeleteProductRequest(productToDelete.Id);

        var mockSet = SetupProducts([.. _defaultProducts, productToDelete]);
        SetupTransactionLines([]);
        SetupOrderLines([]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Assert — an unreferenced product is hard-deleted.
        mockSet.Verify(mock => mock.Remove(It.Is<Product>(e => e.Id == productToDelete.Id)), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenProductReferencedByTransaction()
    {
        // Arrange
        var productToDelete = _builder.ProductBuilder
            .WithId(999)
            .WithImages([])
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        var request = new DeleteProductRequest(productToDelete.Id);

        var mockSet = SetupProducts([.. _defaultProducts, productToDelete]);
        SetupTransactionLines([new TransactionLine
        {
            Id = 1,
            ProductId = productToDelete.Id,
            Quantity = 1,
            UnitPrice = 10m,
            Discount = 0m,
            Product = null!,
            Transaction = null!,
        }]);
        SetupOrderLines([]);

        // Act & Assert — a referenced product cannot be hard-deleted (409, DR-20).
        await Assert.ThrowsAsync<Ombor.Domain.Exceptions.ConflictException>(
            () => _service.DeleteAsync(request));

        mockSet.Verify(mock => mock.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldSetIsArchived_WhenProductExists()
    {
        // Arrange
        var product = _builder.ProductBuilder
            .WithId(999)
            .WithImages([])
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        SetupProducts([.. _defaultProducts, product]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.ArchiveAsync(product.Id);

        // Assert
        Assert.True(product.IsArchived);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ShouldClearIsArchived_WhenProductExists()
    {
        // Arrange
        var product = _builder.ProductBuilder
            .WithId(999)
            .WithImages([])
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        product.IsArchived = true;
        SetupProducts([.. _defaultProducts, product]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.RestoreAsync(product.Id);

        // Assert
        Assert.False(product.IsArchived);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
