using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class CreateProductTests : ProductTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrow(It.IsAny<CreateProductRequest>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Add(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidCreateRequest();
        Product expected = null!;

        _mockContext.Setup(mock => mock.Products.Add(It.Is<Product>(productToAdd => productToAdd.IsEquivalent(request))))
            .Callback<Product>(addedProduct =>
            {
                addedProduct.Category = _builder.CategoryBuilder.BuildAndPopulate();
                SetupProducts([.. _defaultProducts, addedProduct]);
                expected = addedProduct;
            });

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => expected.Id = 99);

        // Act
        var actual = await _service.CreateAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(expected, actual);

        _mockValidator.Verify(mock => mock.ValidateAndThrow(It.IsAny<CreateProductRequest>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Add(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
