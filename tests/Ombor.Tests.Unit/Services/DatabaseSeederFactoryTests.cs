using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.TestDataGenerator.Extensions;
using Ombor.TestDataGenerator.Interfaces;
using Ombor.Tests.Common.Factories;

namespace Ombor.Tests.Unit.Services;

public sealed class DatabaseSeederFactoryTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task CreateSeeder_ShouldSeedNothing_OnALiveHost(string environmentName)
    {
        // Arrange — strict mocks: any read or write the seeder attempted would throw.
        var seeder = CreateFactory(environmentName).CreateSeeder();
        var context = new Mock<IApplicationDbContext>(MockBehavior.Strict);
        var organizationAccessor = new Mock<IOrganizationAccessor>(MockBehavior.Strict);

        // Act
        await seeder.SeedDatabaseAsync(context.Object, organizationAccessor.Object);

        // Assert — no demo organization, user, default password or fabricated payment; nothing touched at all.
        context.VerifyNoOtherCalls();
        organizationAccessor.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void CreateSeeder_ShouldKeepTheDemoSeeder_OffALiveHost(string environmentName)
    {
        var seeder = CreateFactory(environmentName).CreateSeeder();

        Assert.NotEqual("NoDataSeeder", seeder.GetType().Name);
    }

    private static IDatabaseSeederFactory CreateFactory(string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataSeedSettings:Locale"] = "ru",
                ["DataSeedSettings:NumberOfCategories"] = "1",
                ["DataSeedSettings:NumberOfProducts"] = "1",
                ["DataSeedSettings:NumberOfMaxImagesPerProduct"] = "1",
                ["DataSeedSettings:NumberOfPartners"] = "1",
                ["DataSeedSettings:NumberOfTemplatesPerPartner"] = "1",
                ["DataSeedSettings:NumberOfItemsPerTemplate"] = "1",
                ["DataSeedSettings:NumberOfEmployees"] = "1",
                ["DataSeedSettings:NumberOfWarehouses"] = "1",
                ["DataSeedSettings:NumberOfItemsPerWarehouse"] = "1",
                ["DataSeedSettings:NumberOfMaxTransactionsPerPartner"] = "1",
                ["DataSeedSettings:NumberOfOrdersPerCustomer"] = "1",
            })
            .Build();

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        var services = new ServiceCollection()
            .AddSingleton(environment.Object)
            .AddSingleton(Options.Create(FileSettingsFactory.CreateDefault()))
            .AddSingleton(Mock.Of<IImageThumbnailer>())
            .AddSingleton(Mock.Of<IPasswordHasher>())
            .AddTestDataGenerator(configuration);

        return services.BuildServiceProvider().GetRequiredService<IDatabaseSeederFactory>();
    }
}
