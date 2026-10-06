using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Localization;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class DevelopmentDatabaseSeeder(
    DataSeedSettings seedSettings,
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer,
    IPasswordHasher passwordHasher)
    : SeederBase(seedSettings, fileSettings, env, thumbnailer, passwordHasher), IDatabaseSeeder
{
    private readonly PaymentSeedSettings _paymentOptions = seedSettings.PaymentSettings;

    public async Task SeedDatabaseAsync(
        IApplicationDbContext context,
        IOrganizationAccessor organizationAccessor,
        IOrganizationSetupService organizationSetup)
    {
        // Demo data goes into an empty database only. Once any organization exists — demo ones from an earlier start
        // or one registered through the app — a restart leaves the data alone: re-running steps on a filled database
        // fabricated unnumbered, future-dated payments against documents made in the app, and re-extracted the demo
        // images under new names, deleting the files the stored product images point to.
        if (await context.Organizations.AnyAsync())
        {
            return;
        }

        var organizationIds = await EnsureOrganizationsWithUsersAsync(context);
        var nameMap = await EnsureImagesCopiedAsync();

        foreach (var organizationId in organizationIds)
        {
            organizationAccessor.SetOrganization(organizationId);

            await AddCategoriesAsync(context);
            await AddProductsAsync(context);
            await AddProductImagesAsync(context, nameMap);
            await AddPartnersAsync(context);
            await AddTemplatesAsync(context);
            await AddEmployeesAsync(context);
            await EnsureWarehousesAsync(context);
            await AddWalletsAsync(context);
            await SeedTransactionsAsync(context);
            await AddPaymentsAsync(context);
            await AddOrdersAsync(context);

            // Last, so the generators above still see an empty organization: the walk-in customer, main warehouse
            // and other starter rows registration creates (OrganizationSetupService), named in Russian.
            await organizationSetup.SeedStarterDataAsync(organizationId, SupportedLanguages.Russian);
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

    private async Task AddProductImagesAsync(IApplicationDbContext context, Dictionary<string, string> nameMap)
    {
        if (context.ProductImages.Any())
        {
            return;
        }

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

    private async Task AddWalletsAsync(IApplicationDbContext context)
    {
        if (context.Wallets.Any())
        {
            return;
        }

        context.Wallets.Add(new Wallet
        {
            Name = "Касса",
            Type = Domain.Enums.WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();
    }

    private async Task AddPaymentsAsync(IApplicationDbContext context)
    {
        // Seed payments once, like every other step. Re-running on each start fabricated unnumbered, future-dated
        // payments against documents created through the app since the last start.
        if (context.Payments.Any())
        {
            return;
        }

        // Wallet-sourced payments need a wallet to draw from (rule 9).
        var walletId = context.Wallets.Select(w => w.Id).First();

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

            var generated = PaymentGenerator.GeneratePayments(t, walletId, _paymentOptions);
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

    private async Task AddOrdersAsync(IApplicationDbContext context)
    {
        if (context.Orders.Any())
        {
            return;
        }

        var allOrders = new List<Order>();
        var customerIds = context.Partners
            .Where(x => x.Type == Domain.Enums.PartnerType.Customer)
            .Select(x => x.Id)
            .ToArray();
        var products = context.Products
            .Where(x => x.Type != Domain.Enums.ProductType.Supply)
            .ToArray();

        foreach (var customerId in customerIds)
        {
            var orders = OrderGenerator.Generate(
                customerId: customerId,
                products: products,
                seedSettings.NumberOfOrdersPerCustomer);

            allOrders.AddRange(orders);
        }

        context.Orders.AddRange(allOrders);
        await context.SaveChangesAsync();
    }
}
