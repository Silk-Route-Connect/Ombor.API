using Bogus;
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
    private static readonly Faker _faker = new();
    private static readonly Random _random = new();

    public async Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        try
        {
            await AddCategoriesAsync(context);
            await AddProductsAsync(context);
            await AddProductImagesAsync(context);
            await AddPartnersAsync(context);
            await AddTemplatesAsync(context);
            await AddSalesAsync(context);
            await AddSalesWithDebtsAsync(context);
            await AddSuppliesAsync(context);
            await AddSuppliesWithDebtsAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
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

        var products = context.Products
            .Select(x => x.Id)
            .ToArray();
        var templates = TemplateGenerator.Generate(products, seedSettings.NumberOfTemplates, seedSettings.NumberOfItemsPerTemplate, seedSettings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Templates.AddRange(templates);
        await context.SaveChangesAsync();
    }

    private async Task AddSalesAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Sale))
        {
            return;
        }

        var products = context.Products
            .Where(x => x.Type != Domain.Enums.ProductType.Supply)
            .ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Supplier)
            .ToArray();
        var allSales = new List<TransactionRecord>();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(partner.Id, products, Domain.Enums.TransactionType.Sale, true, seedSettings.NumberOfTransactionsPerPartner);
            allSales.AddRange(sales);
        }

        var payments = PaymentGenerator.Generate(allSales);

        context.Payments.AddRange(payments);
        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddSalesWithDebtsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Sale && x.Status == Domain.Enums.TransactionStatus.Open))
        {
            return;
        }

        var products = context.Products
            .Where(x => x.Type != Domain.Enums.ProductType.Supply)
            .ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Supplier)
            .ToArray();
        partners = [.. _faker.Random.ListItems(partners, 10)];
        var allSales = new List<TransactionRecord>();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(partner.Id, products, Domain.Enums.TransactionType.Sale, false, seedSettings.NumberOfTransactionsPerPartner);
            partner.Balance = sales.Sum(x => x.TotalPaid - x.TotalDue);
            allSales.AddRange(sales);
        }

        var payments = PaymentGenerator.Generate(allSales);

        context.Payments.AddRange(payments);
        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddSuppliesAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Supply))
        {
            return;
        }

        var products = context.Products
            .Where(x => x.Type != Domain.Enums.ProductType.Sale)
            .ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Customer)
            .Select(x => x.Id)
            .ToArray();
        var allSupplies = new List<TransactionRecord>();

        foreach (var partner in partners)
        {
            var supplies = TransactionGenerator.Generate(partner, products, Domain.Enums.TransactionType.Supply, true, seedSettings.NumberOfTransactionsPerPartner);
            allSupplies.AddRange(supplies);
        }

        var payments = PaymentGenerator.Generate(allSupplies);

        context.Payments.AddRange(payments);
        context.Transactions.AddRange(allSupplies);
        await context.SaveChangesAsync();
    }

    private async Task AddSuppliesWithDebtsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Supply && x.Status == Domain.Enums.TransactionStatus.Open))
        {
            return;
        }

        var products = context.Products
            .Where(x => x.Type != Domain.Enums.ProductType.Sale)
            .ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Customer)
            .ToArray();
        partners = [.. _faker.Random.ListItems(partners, 15)];
        var allSupplies = new List<TransactionRecord>();

        foreach (var partner in partners)
        {
            var supplies = TransactionGenerator.Generate(partner.Id, products, Domain.Enums.TransactionType.Supply, false, seedSettings.NumberOfTransactionsPerPartner);
            partner.Balance = supplies.Sum(x => x.TotalDue - x.TotalPaid);
            allSupplies.AddRange(supplies);
        }

        var payments = PaymentGenerator.Generate(allSupplies);

        context.Payments.AddRange(payments);
        context.Transactions.AddRange(allSupplies);
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
