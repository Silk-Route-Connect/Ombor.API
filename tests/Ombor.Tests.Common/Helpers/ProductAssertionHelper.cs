using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// Provides assertion helper methods for verifying equivalence between domain entities, request DTOs, and response DTOs in xUnit tests.
/// </summary>
public static class ProductAssertionHelper
{
    /// <summary>
    /// Asserts that a <see cref="Product"/> entity and <see cref="ProductDto"/> have equivalent values for all mapped properties.
    /// </summary>
    /// <param name="expected">The source <see cref="Product"/> entity.</param>
    /// <param name="actual">The <see cref="ProductDto"/> to verify.</param>
    public static void AssertEquivalent(Product? expected, ProductDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
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
    }

    /// <summary>
    /// Asserts that a <see cref="CreateProductRequest"/> and <see cref="CreateProductResponse"/> share identical request values.
    /// </summary>
    /// <param name="request">The original create request.</param>
    /// <param name="response">The response returned by the CreateAsync method.</param>
    public static void AssertEquivalent(CreateProductRequest? request, CreateProductResponse? response)
    {
        Assert.NotNull(request);
        Assert.NotNull(response);

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
        Assert.Equal(request.QuantityInStock <= request.LowStockThreshold, response.IsLowStock);
        Assert.Equal(request.ExpireDate >= GetThresholdDate(), response.IsExpirationClose);
    }

    /// <summary>
    /// Asserts that a <see cref="CreateProductRequest"/> has been correctly mapped to a <see cref="Product"/> entity.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The entity created by the service.</param>
    public static void AssertEquivalent(CreateProductRequest? expected, Product? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Measurement, actual.Measurement.ToString());
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Barcode, actual.Barcode);
        Assert.Equal(expected.SalePrice, actual.SalePrice);
        Assert.Equal(expected.SupplyPrice, actual.SupplyPrice);
        Assert.Equal(expected.RetailPrice, actual.RetailPrice);
        Assert.Equal(expected.QuantityInStock, actual.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, actual.LowStockThreshold);
        Assert.Equal(expected.ExpireDate, actual.ExpireDate);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
    }

    /// <summary>
    /// Asserts that a <see cref="Product"/> entity matches the values returned in a <see cref="CreateProductResponse"/>, including category name.
    /// </summary>
    /// <param name="expected">The saved <see cref="Product"/> entity.</param>
    /// <param name="actual">The response DTO from CreateAsync.</param>
    public static void AssertEquivalent(Product? expected, CreateProductResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

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
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateProductRequest"/> and <see cref="UpdateProductResponse"/> share identical update values.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="response">The response returned by the UpdateAsync method.</param>
    public static void AssertEquivalent(UpdateProductRequest? request, UpdateProductResponse? response)
    {
        Assert.NotNull(request);
        Assert.NotNull(response);

        Assert.Equal(request.Id, response.Id);
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
        Assert.Equal(request.QuantityInStock <= request.LowStockThreshold, response.IsLowStock);
        Assert.Equal(request.ExpireDate >= GetThresholdDate(), response.IsExpirationClose);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateProductRequest"/> has been applied correctly to a <see cref="Product"/> entity.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The entity after ApplyUpdate.</param>
    public static void AssertEquivalent(UpdateProductRequest? expected, Product? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Measurement, actual.Measurement.ToString());
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Barcode, actual.Barcode);
        Assert.Equal(expected.SalePrice, actual.SalePrice);
        Assert.Equal(expected.SupplyPrice, actual.SupplyPrice);
        Assert.Equal(expected.RetailPrice, actual.RetailPrice);
        Assert.Equal(expected.QuantityInStock, actual.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, actual.LowStockThreshold);
        Assert.Equal(expected.ExpireDate, actual.ExpireDate);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
    }

    /// <summary>
    /// Asserts that a <see cref="Product"/> entity matches the values returned in an <see cref="UpdateProductResponse"/>, including category name.
    /// </summary>
    /// <param name="expected">The updated <see cref="Product"/> entity.</param>
    /// <param name="actual">The response DTO from UpdateAsync.</param>
    public static void AssertEquivalent(Product? expected, UpdateProductResponse actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
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
    }

    private static DateOnly GetThresholdDate()
        => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
}
