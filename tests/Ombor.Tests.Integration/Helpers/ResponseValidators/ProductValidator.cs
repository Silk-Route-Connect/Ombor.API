using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Common;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public class ProductValidator(IApplicationDbContext context, FileSettings fileSettings, string webRootPath)
    : ValidatorBase(context, fileSettings, webRootPath)
{
    public async Task ValidateGetAsync(GetProductsRequest request, ProductDto[] response)
    {
        var expectedProducts = await GetProductsAsync(request);

        Assert.Equal(expectedProducts.Length, response.Length);

        for (int i = 0; i < expectedProducts.Length; i++)
        {
            var expected = expectedProducts[i];
            var actual = response[i];

            Assert.Equivalent(expected, actual, true);
        }
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

        var files = actual?.Images?.Select(x => x.FileName) ?? [];
        Assert.All(files, ValidateFileExists);
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

    public async Task ValidateDeleteAsync(int productId, params string[] fileNames)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(c => c.Id == productId);
        var images = await context.ProductImages
            .Where(x => x.ProductId == productId)
            .ToListAsync();

        Assert.Null(product);
        Assert.Empty(images);

        foreach (var fileName in fileNames)
        {
            var matches = Directory.EnumerateFiles(webRootPath, fileName, SearchOption.AllDirectories);
            Assert.Empty(matches);
        }
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

        var products = await query
            .OrderBy(x => x.Name)
            .ToArrayAsync();

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
                x.Packaging.Size == 0 ? null : new ProductPackagingDto(x.Packaging.Size, x.Packaging.Label, x.Packaging.Barcode),
                x.Images.Select(image => new ProductImageDto(image.Id, image.ImageName, image.OriginalUrl, image.ThumbnailUrl)).ToArray()))
            .ToArray();
    }

    private void ValidateFileExists(string fileName)
    {
        var originalPath = Path.Combine(webRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.OriginalsSubfolder, fileName);
        Assert.True(File.Exists(originalPath), $"File '{fileName}' does not exist in the expected location: {originalPath}");

        if (fileSettings.AllowedImageExtensions.Contains(Path.GetExtension(fileName)))
        {
            var thumbnailPath = Path.Combine(webRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.ThumbnailsSubfolder, fileName);
            Assert.True(File.Exists(thumbnailPath), $"File '{fileName}' does not exist in the expected location: {thumbnailPath}");
        }
    }
}
