using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators.Entities;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

public class DevelopmentDatabaseSeeder(DataSeedSettings settings) : IDatabaseSeeder
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

        var categories = CategoryGenerator.Generate(settings.NumberOfCategories)
            .DistinctBy(x => x.Name)
            .ToArray();

        await context.Categories.AddRangeAsync(categories);
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

        if (categories.Length == 0)
        {
            throw new InvalidOperationException("Cannot generate products without categories.");
        }

        var products = ProductGenerator.Generate(categories, settings.NumberOfProducts)
            .DistinctBy(x => x.Name)
            .ToArray();

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
