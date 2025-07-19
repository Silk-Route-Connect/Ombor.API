using Microsoft.AspNetCore.Hosting;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class DevelopmentDatabaseSeeder(
    DataSeedSettings seedSettings,
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer) : IDatabaseSeeder
{
    private static readonly Random _random = new();

    public async Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        await AddCategoriesAsync(context);
        await AddProductsAsync(context);
        await AddProductImagesAsync(context);
        await AddPartnersAsync(context);
        await AddTemplatesAsync(context);
        await AddEmployeesAsync(context);
    }

    private async Task AddCategoriesAsync(IApplicationDbContext context)
    {
        if (context.Categories.Any())
        {
            return;
        }

        var categories = CategoryGenerator.Generate(seedSettings.NumberOfCategories, seedSettings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private async Task AddProductsAsync(IApplicationDbContext context)
    {
        if (context.Products.Any())
        {
            return;
        }

        var categories = context.Categories
            .Select(x => x.Id)
            .ToArray();

        var products = ProductGenerator.Generate(categories, seedSettings.NumberOfProducts, seedSettings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private async Task AddProductImagesAsync(IApplicationDbContext context)
    {
        if (context.ProductImages.Any())
        {
            return;
        }

        // Ensure seed images are in wwwroot and get the map of GUID → original name
        var nameMap = await EnsureImagesCopiedAsync();

        var fileNames = nameMap.Keys.ToArray();
        if (fileNames.Length == 0)
        {
            throw new InvalidOperationException("No seed images were loaded.");
        }

        var productIds = context.Products.Select(p => p.Id).ToArray();
        var images = new List<ProductImage>();

        foreach (var productId in productIds)
        {
            int imagesCount = _random.Next(1, seedSettings.NumberOfMaxImagesPerProduct + 1);

            foreach (var fileName in fileNames.Take(imagesCount))
            {
                images.Add(new ProductImage
                {
                    ProductId = productId,
                    FileName = fileName,
                    ImageName = nameMap[fileName],
                    OriginalUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.OriginalsSubfolder}/{fileName}",
                    ThumbnailUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.ThumbnailsSubfolder}/{fileName}",
                    Product = null! // EF Core will set this automatically
                });
            }
        }

        context.ProductImages.AddRange(images);
        await context.SaveChangesAsync();
    }

    private async Task AddPartnersAsync(IApplicationDbContext context)
    {
        if (context.Partners.Any())
        {
            return;
        }

        var partners = PartnerGenerator.Generate(seedSettings.NumberOfPartners, seedSettings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Partners.AddRange(partners);
        await context.SaveChangesAsync();
    }

    private async Task AddTemplatesAsync(IApplicationDbContext context)
    {
        if (context.Templates.Any())
        {
            return;
        }

        var allTemplates = new List<Template>();
        var partners = context.Partners
            .Select(x => x.Id)
            .ToArray();
        var products = context.Products
            .Select(x => x.Id)
            .ToArray();

        foreach (var partnerId in partners)
        {
            var templates = TemplateGenerator.Generate(
                partnerId,
                products,
                seedSettings.NumberOfTemplatesPerPartner,
                seedSettings.NumberOfItemsPerTemplate,
                seedSettings.Locale)
                .DistinctBy(x => x.Name)
                .ToArray();
            allTemplates.AddRange(templates);
        }

        context.Templates.AddRange(allTemplates);
        await context.SaveChangesAsync();
    }

    private async Task AddEmployeesAsync(IApplicationDbContext context)
    {
        if (context.Employees.Any())
        {
            return;
        }

        var employees = EmployeeGenerator.Generate(seedSettings.NumberOfEmployees, seedSettings.Locale)
            .DistinctBy(x => x.FullName)
            .ToArray();

        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    {
        var originalsDir = Path.Combine(env.WebRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.OriginalsSubfolder);
        var thumbsDir = Path.Combine(env.WebRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.ThumbnailsSubfolder);

        Directory.CreateDirectory(originalsDir);
        Directory.CreateDirectory(thumbsDir);

        if (Directory.EnumerateFiles(originalsDir).Any())
        {
            return [];
        }

        return await ExtractAndSaveSeedImagesAsync(originalsDir, thumbsDir);
    }

    private async Task<Dictionary<string, string>> ExtractAndSaveSeedImagesAsync(string originalsDir, string thumbsDir)
    {
        const string imagesNamespace = "Ombor.TestDataGenerator.Resources.Images.";

        var currentAssembly = typeof(ProductGenerator).Assembly;
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resourceNames = currentAssembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(imagesNamespace, StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var originalFileName = resourceName[imagesNamespace.Length..];
            var extension = Path.GetExtension(originalFileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";

            nameMap[storageFileName] = originalFileName;

            // copy original
            await using var originalImageStream = currentAssembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(resourceName);
            await using var originalImageFileStream = File.Create(Path.Combine(originalsDir, storageFileName));
            await originalImageStream.CopyToAsync(originalImageFileStream);

            // generate & save thumbnail
            originalImageStream.Position = 0;
            var format = ImageHelper.GetThumbnailFormat(extension);
            await using var thumbnailStream = await thumbnailer.GenerateThumbnailAsync(originalImageFileStream, format);
            await using var thumbnailImageFileStream = File.Create(Path.Combine(thumbsDir, storageFileName));
            await thumbnailStream.CopyToAsync(thumbnailImageFileStream);
        }

        return nameMap;
    }
}
