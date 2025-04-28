using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class CreateProductTests : ProductTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = ProductGenerator.GenerateCreateRequest();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<CreateProductRequest>()), Times.Once);
        _mockContext.Verify(c => c.Products.Add(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var request = ProductGenerator.GenerateCreateRequest();
        Product expected = null!;

        _mockContext.Setup(c => c.Products.Add(It.Is<Product>(productToAdd => IsEquivalent(productToAdd, request))))
            .Callback<Product>(addedProduct =>
            {
                addedProduct.Category = _builder.CategoryBuilder.BuildAndPopulate();
                SetupProducts([.. _defaultProducts, addedProduct]);
                expected = addedProduct;
            });
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var actual = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Measurement.ToString(), actual.Measurement);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Barcode, actual.Barcode);
        Assert.Equal(expected.SalePrice, actual.SalePrice);
        Assert.Equal(expected.SupplyPrice, actual.SupplyPrice);
        Assert.Equal(expected.RetailPrice, actual.RetailPrice);
        Assert.Equal(expected.QuantityInStock, actual.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, actual.LowStockThreshold);
        Assert.Equal(expected.ExpireDate, actual.ExpireDate);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
        Assert.Equal(expected.Category.Name, actual.CategoryName);

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<CreateProductRequest>()), Times.Once);
        _mockContext.Verify(c => c.Products.Add(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
