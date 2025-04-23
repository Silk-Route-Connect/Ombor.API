using AutoFixture;
using Castle.Core.Internal;
using FluentValidation;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Services;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using Ombor.TestDataGenerator.Generators.Entities;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Services;

public sealed class ProductServiceTests : ServiceTestsBase
{
    private readonly ProductService _service;
    private readonly Product[] _defaultProducts;

    public ProductServiceTests()
    {
        _defaultProducts = ProductGenerator.Generate([1, 2, 3], 5).ToArray();
        SetupProducts(_defaultProducts);

        _service = new ProductService(_mockContext.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetProductsRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoProducts()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        SetupProducts([]);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAll_WhenNoQueryParameterProvided()
    {
        // Arrange
        var request = new GetProductsRequest(null, null, null, null);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(_defaultProducts.Length, response.Length);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnMatchingProducts_WhenSearchTermIsProvided()
    {
        // Arrange
        var searchTerm = "SearchMatch";
        var request = new GetProductsRequest(searchTerm, null, null, null);

        var matchingProducts = new[]
        {
            new Product
            {
                Name = searchTerm,
                Description = "ProductDescription",
                SKU = "AAA",
                Barcode = "111",
                Measurement = UnitOfMeasurement.Piece,
                QuantityInStock = 100,
                LowStockThreshold = 10,
                ExpireDate = null,
                SalePrice = 150m,
                SupplyPrice = 100m,
                RetailPrice = 175m,
                CategoryId = 1,
                Category = null! // Will be populated in SetupProducts
            },
            new Product
            {
                Name = "ProductName",
                Description = searchTerm,
                SKU = "BBB",
                Barcode = "222",
                Measurement = UnitOfMeasurement.Kilogram,
                QuantityInStock = 200,
                LowStockThreshold = 20,
                ExpireDate = null,
                SalePrice = 250m,
                SupplyPrice = 200m,
                RetailPrice = 275m,
                CategoryId = 2,
                Category = null! // Will be populated in SetupProducts
            }
        };

        SetupProducts([.. _defaultProducts, .. matchingProducts]);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Equal(matchingProducts.Length, result.Length);
        Assert.All(result, actual =>
        {
            var expected = matchingProducts.Find(x => x.Id == actual.Id);
            Assert.NotNull(expected);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Description, actual.Description);
        });
    }

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
        var request = new GetProductByIdRequest(99);
        Product? expected = null;

        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenProductIsFound()
    {
        // Arrange
        var expected = _defaultProducts.PickRandom();
        expected.Category = new Category { Id = expected.CategoryId, Name = "Test Category" };
        var request = new GetProductByIdRequest(expected.Id);

        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act
        var actual = await _service.GetByIdAsync(request);

        // Assert
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Category.Name, actual.CategoryName);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
    }

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
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Never);
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
                SetupProducts([.. _defaultProducts, addedProduct]);
                expected = addedProduct;
                _mockContext.Setup(c => c.Products.FindAsync(addedProduct.Id))
                    .ReturnsAsync(addedProduct);
            });
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var actual = await _service.CreateAsync(request);

        // Assert
        Assert.True(actual.Id > 0);
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
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<UpdateProductRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("err"));

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
        var request = _fixture.Create<UpdateProductRequest>();
        Product? expected = null;
        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var productToUpdate = ProductGenerator.Generate([1, 2, 3]);
        var request = ProductGenerator.GenerateUpdateRequest();

        SetupProducts([.. _defaultProducts, productToUpdate]);

        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync(productToUpdate);

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
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation error."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Never);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();
        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Product>>(
            () => _service.DeleteAsync(request));

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        var toDelete = ProductGenerator.Generate([1, 2]);
        toDelete.Category = new Category { Id = 1, Name = "Test Category 1" };
        var request = _fixture.Create<DeleteProductRequest>();

        _mockContext.Setup(c => c.Products.FindAsync(request.Id))
            .ReturnsAsync(toDelete);
        _mockContext.Setup(c => c.Products.Remove(toDelete));

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Products.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.Products.Remove(It.IsAny<Product>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prepares a mock <see cref="DbSet{Product}"/> for testing by assigning each product:
    /// <list type="bullet">
    ///   <item><description>a unique, sequential <c>Id</c> (since Faker doesn’t generate one),</description></item>
    ///   <item><description>a corresponding <see cref="Category"/> instance whose <c>Id</c> matches the product’s <c>CategoryId</c>.</description></item>
    /// </list>
    /// Once configured, the populated list is converted into a mock <see cref="DbSet{Product}"/> and hooked up on <c>_mockContext.Products</c>.
    /// </summary>
    /// <param name="products">
    /// The initial sequence of <see cref="Product"/> instances to configure and expose via the mock context.
    /// </param>
    /// <remarks>
    /// Uses <see cref="CategoryGenerator.Generate"/> to create category data, and
    /// <c>BuildMockDbSet()</c> from MockQueryable.Moq to produce a testable <see cref="DbSet{Product}"/>.
    /// </remarks>
    private void SetupProducts(IEnumerable<Product> products)
    {
        var list = products.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            var currentProduct = list[i];
            currentProduct.Id = i + 1; // Faker does not generate IDs, so assign sequentially starting at 1

            // Generate and assign a Category whose Id matches the product's CategoryId
            var generatedCategory = CategoryGenerator.Generate();
            currentProduct.Category = new Category
            {
                Id = currentProduct.CategoryId,
                Name = generatedCategory.Name,
                Description = generatedCategory.Description,
            };
        }

        // Build and register the mock DbSet<Product>
        var mockSet = list.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Products).Returns(mockSet.Object);
    }

    private static bool IsEquivalent(Product product, CreateProductRequest request) =>
        product.Name == request.Name &&
        product.SKU == request.SKU &&
        product.Description == request.Description &&
        product.Barcode == request.Barcode &&
        product.SalePrice == request.SalePrice &&
        product.SupplyPrice == request.SupplyPrice &&
        product.RetailPrice == request.RetailPrice &&
        product.QuantityInStock == request.QuantityInStock &&
        product.LowStockThreshold == request.LowStockThreshold &&
        product.Measurement.ToString() == request.Measurement &&
        product.ExpireDate == request.ExpireDate &&
        product.CategoryId == request.CategoryId;
}
