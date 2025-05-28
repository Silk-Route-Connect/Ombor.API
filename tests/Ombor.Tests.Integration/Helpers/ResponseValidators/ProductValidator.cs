using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Tests.Common.Helpers;

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
        Assert.Equal(expected.Type.ToString(), response.Type);
        Assert.Equal(expected.CategoryId, response.CategoryId);
        Assert.Equal(expected.Category.Name, response.CategoryName);
    }

    public async Task ValidatePostAsync(CreateProductRequest request, CreateProductResponse response)
    {
        var actual = await context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(c => c.Id == response.Id);

        ProductAssertionHelper.AssertEquivalent(request, actual);
        ProductAssertionHelper.AssertEquivalent(request, response);
        ProductAssertionHelper.AssertEquivalent(actual, response);
    }

    public async Task ValidatePutAsync(UpdateProductRequest request, UpdateProductResponse response)
    {
        var actual = await context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        ProductAssertionHelper.AssertEquivalent(request, actual);
        ProductAssertionHelper.AssertEquivalent(request, response);
        ProductAssertionHelper.AssertEquivalent(actual, response);
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

        var products = await query.ToArrayAsync();

        return products
            .Select(x => new ProductDto(
                x.Id,
                x.CategoryId,
                x.Category.Name,
                x.Name,
                x.SKU,
                x.Description,
                x.Barcode,
                x.SalePrice,
                x.SupplyPrice,
                x.RetailPrice,
                x.QuantityInStock,
                x.LowStockThreshold,
                x.QuantityInStock <= x.LowStockThreshold,
                x.Measurement.ToString(),
                x.Type.ToString(),
                x.Images.Select(image => new ProductImageDto(image.Id, image.Name, image.OriginalUrl, image.ThumbnailUrl)).ToArray()))
            .ToArray();
    }
}
