using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Tests.Integration.Endpoints;
using Ombor.Tests.Integration.Helpers;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Seeders;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Seeding;

/// <summary>
/// The development seed fills an empty database once; a restart against a database that already holds organizations
/// must add nothing (backend-8 remainder: it used to fabricate payments and re-extract demo images on every start).
/// </summary>
public sealed class DevelopmentSeedTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => string.Empty;

    protected override string GetUrl(int id) => string.Empty;

    [Fact]
    public async Task DevelopmentSeed_OnADatabaseWithOrganizations_AddsNothing()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<IApplicationDbContext>();
        var seeder = new DevelopmentDatabaseSeeder(
            services.GetRequiredService<IOptions<DataSeedSettings>>().Value,
            services.GetRequiredService<IOptions<FileSettings>>().Value,
            services.GetRequiredService<IWebHostEnvironment>(),
            services.GetRequiredService<IImageThumbnailer>(),
            services.GetRequiredService<IPasswordHasher>());
        var before = await CountsAsync(context);
        Assert.True(before.Organizations > 0);

        await seeder.SeedDatabaseAsync(
            context,
            services.GetRequiredService<IOrganizationAccessor>(),
            services.GetRequiredService<IOrganizationSetupService>());

        Assert.Equal(before, await CountsAsync(context));
    }

    private static async Task<Counts> CountsAsync(IApplicationDbContext context) => new(
        await context.Organizations.CountAsync(),
        await context.Users.IgnoreQueryFilters().CountAsync(),
        await context.Products.IgnoreQueryFilters().CountAsync(),
        await context.ProductImages.IgnoreQueryFilters().CountAsync(),
        await context.Partners.IgnoreQueryFilters().CountAsync(),
        await context.Transactions.IgnoreQueryFilters().CountAsync(),
        await context.Payments.IgnoreQueryFilters().CountAsync(),
        await context.Orders.IgnoreQueryFilters().CountAsync(),
        await context.Wallets.IgnoreQueryFilters().CountAsync());

    private sealed record Counts(
        int Organizations, int Users, int Products, int ProductImages, int Partners, int Transactions, int Payments, int Orders, int Wallets);
}
