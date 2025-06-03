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
    public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        var productToDelete = _builder.ProductBuilder
            .WithId(999)
            .WithImages([]) // Without images
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        var request = new DeleteProductRequest(productToDelete.Id);

        var mockSet = SetupProducts([.. _defaultProducts, productToDelete]);
        mockSet.Setup(mock => mock.Remove(It.Is<Product>(e => e.Id == productToDelete.Id)));

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(mock => mock.Remove(It.Is<Product>(e => e.Id == productToDelete.Id)), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Exactly(2));
        _mockFileService.Verify(mock => mock.DeleteAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFiles_WhenProductHasImages()
    {
        // Arrange
        var productToDelete = _builder.ProductBuilder
            .WithId(999)
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        var imagesToDelete = productToDelete.Images.Select(x => x.FileName).ToArray();
        var request = new DeleteProductRequest(productToDelete.Id);

        var mockSet = SetupProducts([.. _defaultProducts, productToDelete]);
        mockSet.Setup(mock => mock.Remove(It.Is<Product>(e => e.Id == productToDelete.Id)));

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockFileService.Setup(mock => mock.DeleteAsync(imagesToDelete, _fileSettings.ProductUploadsSection, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(mock => mock.Remove(It.Is<Product>(e => e.Id == productToDelete.Id)), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Exactly(2));
        _mockFileService.Verify(mock => mock.DeleteAsync(imagesToDelete, _fileSettings.ProductUploadsSection, It.IsAny<CancellationToken>()), Times.Once);
    }
}
