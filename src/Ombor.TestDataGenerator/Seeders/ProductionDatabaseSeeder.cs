using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class ProductionDatabaseSeeder(DataSeedSettings settings) : IDatabaseSeeder
{
    public async Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        await AddCategoriesAsync(context);
        await AddProductsAsync(context);
    }

    private async Task AddCategoriesAsync(IApplicationDbContext context)
    {
        if (context.Categories.Any())
        {
            return;
        }

        var categories = CategoryGenerator.Generate(settings.NumberOfCategories, settings.Locale)
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

        var products = ProductGenerator.Generate(categories, settings.NumberOfProducts, settings.Locale)
            .DistinctBy(x => x.Name)
            .ToArray();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
