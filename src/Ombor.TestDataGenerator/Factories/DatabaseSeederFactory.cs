using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Interfaces;
using Ombor.TestDataGenerator.Seeders;

namespace Ombor.TestDataGenerator.Factories;

internal sealed class DatabaseSeederFactory(IServiceScopeFactory serviceScopeFactory, IWebHostEnvironment environment) : IDatabaseSeederFactory
{
    public IDatabaseSeeder CreateSeeder()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<DataSeedSettings>>().Value;

        return environment.EnvironmentName.ToLowerInvariant() switch
        {
            "development" => new DevelopmentDatabaseSeeder(settings),
            "testing" => new TestingDatabaseSeeder(settings),
            "production" => new ProductionDatabaseSeeder(settings),
            "staging" => new ProductionDatabaseSeeder(settings),
            _ => throw new ArgumentOutOfRangeException($"Cannot create an instance of Database Seeder for environment: {environment.EnvironmentName}."),
        };
    }
}
