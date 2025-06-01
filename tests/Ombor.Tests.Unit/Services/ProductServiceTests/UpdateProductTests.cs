using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Application.Models;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class UpdateProductTests : ProductTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateInvalidUpdateRequest(ProductId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation Errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockContext.Verify(mock => mock.Products, Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var (productToUpdate, category) = CreateProductWithCategory();
        var request = ProductRequestFactory.GenerateValidUpdateRequest(productToUpdate.Id, category.Id);

        SetupProducts([.. _defaultProducts, productToUpdate]);
        SetupCategories([category]);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(request, response);
        ProductAssertionHelper.AssertEquivalent(request, productToUpdate);
        ProductAssertionHelper.AssertEquivalent(productToUpdate, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFiles_WhenProductHasImages()
    {
        // Arrange
        var productToUpdate = _builder.ProductBuilder
            .WithId(ProductId)
            .WithCategory(_defaultCategory)
            .BuildAndPopulate();
        var categoryForUpdate = _builder.CategoryBuilder
            .WithId(999)
            .BuildAndPopulate();
        var imagesToDelete = productToUpdate.Images.Take(2);
        var filesToDelete = imagesToDelete.Select(x => x.FileName).ToArray();
        var request = ProductRequestFactory.GenerateValidUpdateRequestWithAttachments(
            ProductId,
            categoryForUpdate.Id,
            [.. imagesToDelete.Select(x => x.Id)]);
        var fileUploadResults = request.Attachments!
            .Select(x => new FileUploadResult(x.FileName, x.FileName, $"originals/{x.FileName}", $"thumbnails/{x.FileName}"))
            .ToArray();

        SetupProducts([.. _defaultProducts, productToUpdate]);
        SetupCategories([_defaultCategory, categoryForUpdate]);
        SetupProductImages([.. productToUpdate.Images]);

        _mockFileService.Setup(mock => mock.DeleteAsync(filesToDelete, _fileSettings.ProductUploadsSection, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockFileService.Setup(mock => mock.UploadAsync(request.Attachments!, _fileSettings.ProductUploadsSection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileUploadResults);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert   
        ProductAssertionHelper.AssertEquivalent(request, response);
        ProductAssertionHelper.AssertEquivalent(request, productToUpdate);
        ProductAssertionHelper.AssertEquivalent(productToUpdate, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);
        _mockContext.Verify(mock => mock.ProductImages, Times.Once);
        _mockFileService.Verify(
            mock => mock.UploadAsync(
                request.Attachments!,
                _fileSettings.ProductUploadsSection,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockFileService.Verify(
            mock => mock.DeleteAsync(
                filesToDelete,
                _fileSettings.ProductUploadsSection,
                It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyNoOtherCalls();
    }
}
