using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public class ProductValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetProductsRequest request, ProductDto[] response)
    {
        var expectedProducts = await GetProductsAsync(request);

        Assert.Equal(expectedProducts.Length, response.Length);
        Assert.All(expectedProducts, expected =>
        {
            var actual = response.FirstOrDefault(c => c.Id == expected.Id);

            Assert.NotNull(actual);
            Assert.Equivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int productId, ProductDto response)
    {
        var expected = await context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(c => c.Id == productId);

        Assert.NotNull(expected);
        Assert.Equal(expected.Name, response.Name);
        Assert.Equal(expected.Description, response.Description);
        Assert.Equal(expected.SKU, response.SKU);
        Assert.Equal(expected.Barcode, response.Barcode);
        Assert.Equal(expected.SalePrice, response.SalePrice);
        Assert.Equal(expected.SupplyPrice, response.SupplyPrice);
        Assert.Equal(expected.RetailPrice, response.RetailPrice);
        Assert.Equal(expected.QuantityInStock, response.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, response.LowStockThreshold);
        Assert.Equal(expected.Measurement.ToString(), response.Measurement);
        Assert.Equal(expected.ExpireDate, response.ExpireDate);
        Assert.Equal(expected.CategoryId, response.CategoryId);
        Assert.Equal(expected.Category.Name, response.CategoryName);
    }

    public async Task ValidatePostAsync(CreateProductRequest request, CreateProductResponse response)
    {
        var actual = await context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(c => c.Id == response.Id);

        Assert.NotNull(actual);
        AssertEquivalent(request, actual);
        AssertEquivalent(actual, response);
    }

    public async Task ValidatePutAsync(UpdateProductRequest request, UpdateProductResponse response)
    {
        var actual = await context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        Assert.NotNull(actual);
        AssertEquivalent(request, actual);
        AssertEquivalent(actual, response);
    }

    public async Task ValidateDeleteAsync(int productId)
    {
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == productId);

        Assert.Null(product);
    }

    private async Task<ProductDto[]> GetProductsAsync(GetProductsRequest request)
    {
        var query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(request.SearchTerm) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm)) ||
                (x.SKU != null && x.SKU.Contains(request.SearchTerm)) ||
                (x.Barcode != null && x.Barcode.Contains(request.SearchTerm)));
        }
        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.SalePrice <= request.MaxPrice.Value);
        }
        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.SalePrice >= request.MinPrice.Value);
        }
        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        return await query
            .Select(x => new ProductDto(
                x.Id,
                x.CategoryId,
                x.Category.Name,
                x.Name,
                x.SKU,
                x.Measurement.ToString(),
                x.Description,
                x.Barcode,
                x.SalePrice,
                x.SupplyPrice,
                x.RetailPrice,
                x.QuantityInStock,
                x.LowStockThreshold,
                x.ExpireDate,
                x.QuantityInStock <= x.LowStockThreshold,
                x.ExpireDate >= thresholdDate))
            .ToArrayAsync();
    }

    private static void AssertEquivalent(CreateProductRequest expected, Product actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Barcode, actual.Barcode);
        Assert.Equal(expected.SalePrice, actual.SalePrice);
        Assert.Equal(expected.SupplyPrice, actual.SupplyPrice);
        Assert.Equal(expected.RetailPrice, actual.RetailPrice);
        Assert.Equal(expected.QuantityInStock, actual.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, actual.LowStockThreshold);
        Assert.Equal(expected.Measurement, actual.Measurement.ToString());
        Assert.Equal(expected.ExpireDate, actual.ExpireDate);
    }

    private static void AssertEquivalent(Product expected, CreateProductResponse actual)
    {
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
        Assert.Equal(expected.QuantityInStock <= expected.LowStockThreshold, actual.IsLowStock);
    }

    private static void AssertEquivalent(UpdateProductRequest expected, Product actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SKU, actual.SKU);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Barcode, actual.Barcode);
        Assert.Equal(expected.SalePrice, actual.SalePrice);
        Assert.Equal(expected.SupplyPrice, actual.SupplyPrice);
        Assert.Equal(expected.RetailPrice, actual.RetailPrice);
        Assert.Equal(expected.QuantityInStock, actual.QuantityInStock);
        Assert.Equal(expected.LowStockThreshold, actual.LowStockThreshold);
        Assert.Equal(expected.Measurement, actual.Measurement.ToString());
        Assert.Equal(expected.ExpireDate, actual.ExpireDate);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
    }

    private static void AssertEquivalent(Product expected, UpdateProductResponse actual)
    {
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
}
