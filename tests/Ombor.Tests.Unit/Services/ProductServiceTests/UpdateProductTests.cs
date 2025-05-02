using System.ComponentModel.DataAnnotations;
using Moq;
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
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidUpdateRequest(ProductId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var category = _builder.CategoryBuilder
            .BuildAndPopulate();
        var productToUpdate = _builder.ProductBuilder
            .WithId(ProductId)
            .WithCategory(category)
            .BuildAndPopulate();
        var request = ProductRequestFactory.GenerateValidUpdateRequest(productToUpdate.Id, category.Id);

        SetupProducts([.. _defaultProducts, productToUpdate]);
        SetupCategories([category]);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(request, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
