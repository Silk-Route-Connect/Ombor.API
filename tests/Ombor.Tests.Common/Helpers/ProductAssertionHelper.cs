using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class ProductAssertionHelper
{
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

    public static void AssertEquivalent(UpdateProductRequest? request, UpdateProductResponse? response)
    {
        Assert.NotNull(request);
        Assert.NotNull(response);

        Assert.Equal(request.Id, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.SKU, response.SKU);
        Assert.Equal(request.Measurement.ToString(), response.Measurement);
        Assert.Equal(request.Description, response.Description);
        Assert.Equal(request.Barcode, response.Barcode);
        Assert.Equal(request.SalePrice, response.SalePrice);
        Assert.Equal(request.SupplyPrice, response.SupplyPrice);
        Assert.Equal(request.RetailPrice, response.RetailPrice);
        Assert.Equal(request.QuantityInStock, response.QuantityInStock);
        Assert.Equal(request.LowStockThreshold, response.LowStockThreshold);
        Assert.Equal(request.ExpireDate, response.ExpireDate);
        Assert.Equal(request.CategoryId, response.CategoryId);
    }
}
