using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
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

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation Errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<UpdateProductRequest>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidUpdateRequest(ProductId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<UpdateProductRequest>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var productToUpdate = _builder.ProductBuilder
            .WithId(ProductId)
            .BuildAndPopulate();
        var request = ProductRequestFactory.GenerateValidUpdateRequest(productToUpdate.Id);

        SetupProducts([.. _defaultProducts, productToUpdate]);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(request, response);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
