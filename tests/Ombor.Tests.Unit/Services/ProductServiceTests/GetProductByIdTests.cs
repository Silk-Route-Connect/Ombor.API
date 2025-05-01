using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class GetProductByIdTests : ProductTestsBase
{
    private const int ProductId = 100;

    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetProductByIdRequest(ProductId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetProductByIdRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new GetProductByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetProductByIdRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenProductIsFound()
    {
        // Arrange
        var expected = _builder.ProductBuilder
            .WithId(ProductId)
            .BuildAndPopulate();
        var request = new GetProductByIdRequest(expected.Id);

        SetupProducts([.. _defaultProducts, expected]);

        // Act
        var actual = await _service.GetByIdAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(expected, actual);

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetProductByIdRequest>()), Times.Once);
    }
}
