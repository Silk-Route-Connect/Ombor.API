using Ombor.Application.Interfaces;

namespace Ombor.TestDataGenerator.Interfaces;

public interface IDatabaseSeeder
{
    Task SeedDatabaseAsync(IApplicationDbContext context);
}
