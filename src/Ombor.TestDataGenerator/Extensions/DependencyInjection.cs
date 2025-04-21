using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Factories;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddTestDataGenerator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDatabaseSeederFactory, DatabaseSeederFactory>();
        services.AddConfigurationSettings(configuration);

        return services;
    }

    private static void AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DataSeedSettings>()
            .Bind(configuration.GetSection(nameof(DataSeedSettings)))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
