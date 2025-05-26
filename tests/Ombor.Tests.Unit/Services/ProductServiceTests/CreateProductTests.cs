using System.ComponentModel.DataAnnotations;
using Moq;
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

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Add(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidCreateRequest();
        Product? addedProduct = null;

        _mockContext.Setup(mock => mock.Products.Add(It.Is<Product>(product => product.IsEquivalent(request))))
            .Callback<Product>(capturedProduct =>
            {
                capturedProduct.Category = _builder.CategoryBuilder.BuildAndPopulate();
                SetupProducts([.. _defaultProducts, capturedProduct]);
                addedProduct = capturedProduct;
            });

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() =>
            {
                if (addedProduct is not null)
                {
                    addedProduct.Id = 99;
                }
            });

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        ProductAssertionHelper.AssertEquivalent(request, response);
        ProductAssertionHelper.AssertEquivalent(request, addedProduct);
        ProductAssertionHelper.AssertEquivalent(addedProduct, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Products.Add(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
