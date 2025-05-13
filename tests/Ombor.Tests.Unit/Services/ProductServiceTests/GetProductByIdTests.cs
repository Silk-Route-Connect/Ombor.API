using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class GetProductByIdTests : ProductTestsBase
{
    [Fact(Skip = "Test")]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetProductByIdRequest(ProductId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact(Skip = "Test")]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new GetProductByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact(Skip = "Test")]
    public async Task GetByIdAsync_ShouldReturnDto_WhenProductIsFound()
    {
        // Arrange
        var (expected, _) = CreateProductWithCategory();
        var request = new GetProductByIdRequest(expected.Id);

        SetupProducts([.. _defaultProducts, expected]);

        // Act
        var response = await _service.GetByIdAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(expected, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products, Times.Once);

        VerifyNoOtherCalls();
    }
}
