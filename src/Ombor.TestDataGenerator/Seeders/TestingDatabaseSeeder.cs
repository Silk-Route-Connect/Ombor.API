using Bogus;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class TestingDatabaseSeeder(DataSeedSettings settings) : IDatabaseSeeder
{
    private readonly Faker _faker = new(settings.Locale);

    public async Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        await CreateCategoriesAsync(context);
        await CreateProductsAsync(context);
    }

    private async Task CreateCategoriesAsync(IApplicationDbContext context)
    {
        if (context.Categories.Any())
        {
            return;
        }

        var categories = Enumerable.Range(1, settings.NumberOfCategories)
            .Select(i => new Category
            {
                Name = $"Test Category {i}",
                Description = _faker.Lorem.Sentence(),
            });

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private async Task CreateProductsAsync(IApplicationDbContext context)
    {
        if (context.Products.Any())
        {
            return;
        }

        var categoryIds = context.Categories.Select(i => i.Id);

        var products = Enumerable.Range(1, settings.NumberOfProducts)
            .Select(i => new Product
            {
                Name = $"Test Product {i}",
                SKU = _faker.Random.Guid().ToString(),
                Description = _faker.Commerce.ProductDescription(),
                Barcode = _faker.Commerce.Ean13(),
                SalePrice = _faker.Finance.Amount(),
                SupplyPrice = _faker.Finance.Amount(),
                RetailPrice = _faker.Finance.Amount(),
                QuantityInStock = _faker.Random.Number(),
                LowStockThreshold = _faker.Random.Number(),
                Measurement = _faker.Random.Enum<UnitOfMeasurement>(),
                ExpireDate = _faker.Date.FutureDateOnly(),
                CategoryId = _faker.PickRandom<int>(categoryIds),
                Category = null!
            });

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
