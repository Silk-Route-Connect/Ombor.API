using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
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
    IImageThumbnailer thumbnailer) : SeederBase(fileSettings, env, thumbnailer), IDatabaseSeeder
{
    private readonly PaymentSeedSettings _paymentOptions = seedSettings.PaymentSettings;

    public async Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        await AddCategoriesAsync(context);
        await AddProductsAsync(context);
        await AddProductImagesAsync(context);
        await AddPartnersAsync(context);
        await AddTemplatesAsync(context);
        await AddInventoriesAsync(context);
        await AddSalesAsync(context);
        await AddSuppliesAsync(context);
        await AddSaleRefundsAsync(context);
        await AddSupplyRefundsAsync(context);
        await AddPaymentsAsync(context);
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

    private async Task AddInventoriesAsync(IApplicationDbContext context)
    {
        if (context.Inventories.Any())
        {
            return;
        }

        var products = context.Products
            .Select(x => x.Id)
            .ToArray();
        var inventories = InventoryGenerator.Generate(
            products,
            seedSettings.NumberOfItemsPerInventory,
            seedSettings.NumberOfInventories,
            seedSettings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Inventories.AddRange(inventories);
        await context.SaveChangesAsync();
    }

    //private async Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    private async Task AddSalesAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Sale))
        {
            return;
        }

        var allSales = new List<TransactionRecord>();
        var products = context.Products.ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Supplier)
            .ToArray();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(
                partner.Id,
                Domain.Enums.TransactionType.Sale,
                products,
                seedSettings.NumberOfMaxTransactionsPerPartner);

            allSales.AddRange(sales);
        }

        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddSuppliesAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.Supply))
        {
            return;
        }

        var allSales = new List<TransactionRecord>();
        var products = context.Products.ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Customer)
            .ToArray();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(
                partner.Id,
                Domain.Enums.TransactionType.Supply,
                products,
                seedSettings.NumberOfMaxTransactionsPerPartner);

            allSales.AddRange(sales);
        }

        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddSaleRefundsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.SaleRefund))
        {
            return;
        }

        var allSales = new List<TransactionRecord>();
        var products = context.Products.ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Supplier)
            .ToArray();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(
                partner.Id,
                Domain.Enums.TransactionType.SaleRefund,
                products,
                seedSettings.NumberOfMaxTransactionsPerPartner);

            allSales.AddRange(sales);
        }

        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddSupplyRefundsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any(x => x.Type == Domain.Enums.TransactionType.SupplyRefund))
        {
            return;
        }

        var allSales = new List<TransactionRecord>();
        var products = context.Products.ToArray();
        var partners = context.Partners
            .Where(x => x.Type != Domain.Enums.PartnerType.Supplier)
            .ToArray();

        foreach (var partner in partners)
        {
            var sales = TransactionGenerator.Generate(
                partner.Id,
                Domain.Enums.TransactionType.SupplyRefund,
                products,
                seedSettings.NumberOfMaxTransactionsPerPartner);

            allSales.AddRange(sales);
        }

        context.Transactions.AddRange(allSales);
        await context.SaveChangesAsync();
    }

    private async Task AddPaymentsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any())
        {
            return;
        }

        // Load all transactions that do not yet have any allocations OR still have unpaid amounts
        var transactions = await context.Transactions
            .Include(t => t.PaymentAllocations)
            .Include(t => t.Partner)
            .Include(t => t.Lines)
            .Where(t => t.TotalDue > 0) // skip weird zero-due transactions
            .ToListAsync();

        var allPayments = new List<Payment>();

        foreach (var t in transactions)
        {
            // If already fully paid, skip (or regenerate if you want)
            if (t.UnpaidAmount == 0) continue;

            var generated = PaymentGenerator.GeneratePayments(t, _paymentOptions);
            if (generated.Count == 0) continue;

            allPayments.AddRange(generated);
        }

        if (allPayments.Count > 0)
        {
            context.Payments.AddRange(allPayments);

            // Ensure transaction aggregates & statuses are persisted
            context.Transactions.UpdateRange(transactions);

            await context.SaveChangesAsync();
        }
    }
}
