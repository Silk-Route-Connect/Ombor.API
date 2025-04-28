using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class GetProductByIdTests : ProductTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<GetProductByIdRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetProductByIdRequest>()), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = new GetProductByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenProductIsFound()
    {
        // Arrange
        var expected = _defaultProducts.PickRandom();
        expected.Category = new Category { Id = expected.CategoryId, Name = "Test Category" };
        SetupProducts([.. _defaultProducts, expected]);
        var request = new GetProductByIdRequest(expected.Id);

        // Act
        var actual = await _service.GetByIdAsync(request);

        // Assert
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Category.Name, actual.CategoryName);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
    }
}
