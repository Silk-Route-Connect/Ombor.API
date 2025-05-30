using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces.File;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Interfaces;
using Ombor.TestDataGenerator.Seeders;

namespace Ombor.TestDataGenerator.Factories;

internal sealed class DatabaseSeederFactory(IServiceScopeFactory serviceScopeFactory, IWebHostEnvironment environment) : IDatabaseSeederFactory
{
    public IDatabaseSeeder CreateSeeder()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var seedSettings = scope.ServiceProvider.GetRequiredService<IOptions<DataSeedSettings>>().Value;
        var fileSettings = scope.ServiceProvider.GetRequiredService<IOptions<FileSettings>>().Value;
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var imageThumbnailer = scope.ServiceProvider.GetRequiredService<IImageThumbnailer>();

        return environment.EnvironmentName.ToLowerInvariant() switch
        {
            "development" => new DevelopmentDatabaseSeeder(seedSettings, fileSettings, hostEnvironment, imageThumbnailer),
            "testing" => new TestingDatabaseSeeder(seedSettings),
            "production" => new ProductionDatabaseSeeder(seedSettings),
            "staging" => new ProductionDatabaseSeeder(seedSettings),
            _ => throw new ArgumentOutOfRangeException($"Cannot create an instance of Database Seeder for environment: {environment.EnvironmentName}."),
        };
    }
}
