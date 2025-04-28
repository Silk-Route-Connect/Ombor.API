using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.TestDataGenerator.Generators.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class UpdateProductTests : ProductTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<UpdateProductRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation Errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = _fixture.Build<UpdateProductRequest>()
            .With(x => x.Id, NonExistentEntityId)
            .Create();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var request = ProductGenerator.GenerateUpdateRequest();
        var productToUpdate = _builder.ProductBuilder.BuildAndPopulate();

        SetupProducts([.. _defaultProducts, productToUpdate]);

        request = request with { Id = productToUpdate.Id };

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.SKU, response.SKU);
        Assert.Equal(request.Measurement, response.Measurement);
        Assert.Equal(request.Description, response.Description);
        Assert.Equal(request.Barcode, response.Barcode);
        Assert.Equal(request.SalePrice, response.SalePrice);
        Assert.Equal(request.SupplyPrice, response.SupplyPrice);
        Assert.Equal(request.RetailPrice, response.RetailPrice);
        Assert.Equal(request.QuantityInStock, response.QuantityInStock);
        Assert.Equal(request.LowStockThreshold, response.LowStockThreshold);
        Assert.Equal(request.ExpireDate, response.ExpireDate);
        Assert.Equal(request.CategoryId, response.CategoryId);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
