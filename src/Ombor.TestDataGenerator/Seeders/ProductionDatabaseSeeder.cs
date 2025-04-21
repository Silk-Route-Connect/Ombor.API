using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

public sealed class ProductionDatabaseSeeder(DataSeedSettings settings) : IDatabaseSeeder
{
    public Task SeedDatabaseAsync(IApplicationDbContext context)
    {
        throw new NotImplementedException();
    }
}
