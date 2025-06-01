using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class ProductTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected const int DefaultCategoryId = 5;

    protected override string GetUrl()
        => Routes.Product;

    protected override string GetUrl(int id)
        => $"{Routes.Product}/{id}";

    protected async Task<int> CreateProductAsync(int categoryId, params string[] imageFileNames)
    {
        var product = new Product
        {
            Name = "Test Product",
            SKU = $"Test SKU {Guid.NewGuid()}",
            Barcode = "Test Barcode",
            Description = "Test Product Description",
            SalePrice = 100.00m,
            SupplyPrice = 50.00m,
            RetailPrice = 90.00m,
            LowStockThreshold = 10,
            QuantityInStock = 100,
            Measurement = UnitOfMeasurement.Unit,
            CategoryId = categoryId,
            Category = null!
        };
        var images = imageFileNames?.Length > 0
            ? CreateImages(product.Id, imageFileNames)
            : [];
        product.Images.AddRange(images);

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    protected async Task<Product> CreateProductAsync(Product product, params string[] imageFileNames)
    {
        var images = imageFileNames?.Length > 0
            ? CreateImages(product.Id, imageFileNames)
            : [];
        product.Images.AddRange(images);

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    private List<ProductImage> CreateImages(int productId, string[] imageFileNames)
    {
        var services = _factory.Services;
        var fileSettings = services.GetRequiredService<IOptions<FileSettings>>().Value;
        var webRoot = services.GetRequiredService<IWebHostEnvironment>().WebRootPath;

        string originalsDir = Path.Combine(webRoot, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.OriginalsSubfolder);
        string thumbsDir = Path.Combine(webRoot, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.ThumbnailsSubfolder);
        Directory.CreateDirectory(originalsDir);
        Directory.CreateDirectory(thumbsDir);

        var savedImages = new List<ProductImage>();

        foreach (var fileName in imageFileNames)
        {
            CopySeedImage(fileName, originalsDir);
            CopySeedImage(fileName, thumbsDir);

            string originalUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.OriginalsSubfolder}/{fileName}";
            string thumbnailUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.ThumbnailsSubfolder}/{fileName}";

            var image = new ProductImage
            {
                FileName = fileName,
                ImageName = fileName,
                OriginalUrl = originalUrl,
                ThumbnailUrl = thumbnailUrl,
                ProductId = productId,
                Product = null!,
            };
            savedImages.Add(image);
        }

        return savedImages;
    }

    private static void CopySeedImage(string fileName, string destinationDirectory)
    {
        using var stream = ImageProvider.GetImageStream(fileName);
        var destinationPath = Path.Combine(destinationDirectory, fileName);

        using var fileStream = File.Create(destinationPath);
        stream.CopyTo(fileStream);
    }
}
