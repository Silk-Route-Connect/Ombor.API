using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class ProductionDatabaseSeeder(DataSeedSettings settings) : IDatabaseSeeder
{
    public Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        Console.WriteLine(settings.ToString());
        return Task.CompletedTask;
    }
}
