using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Ombor.Application.Configurations;
using Ombor.Domain.Exceptions;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Services;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Concurrency;

/// <summary>
/// The lock itself: a write that cannot get its organization's lock in time is refused with 409 <c>conflict.busy</c>
/// and records nothing, a finished write releases the lock, and one organization never waits for another.
/// </summary>
public sealed class OrganizationWriteLockTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ConcurrencyTestsBase(factory, output)
{
    private const int TestOrganizationId = 1;

    [Fact]
    public async Task Write_IsRefusedWithConflictBusy_WhileTheLockIsHeldPastTheTimeout_AndPassesOnceReleased()
    {
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 5);

        // A 1-second wait so the test does not sit through the default 10 seconds.
        using var quickHost = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WriteLockSettings.SectionName}:{nameof(WriteLockSettings.TimeoutSeconds)}"] = "1",
                })));
        using var client = CreateClient(quickHost);
        HttpRequestMessage Decrease() => Json(Routes.StockAdjustment, new
        {
            warehouseId,
            productId,
            direction = "Decrease",
            quantity = 1m,
            reason = "Damage",
        });

        await using (var holder = CreateContext(TestOrganizationId))
        await using (await CreateLock(holder).BeginOrgWriteAsync())
        {
            var refused = await client.SendAsync(Decrease());

            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal(ErrorCodes.ConflictBusy, (string?)JObject.Parse(await refused.Content.ReadAsStringAsync())["code"]);
        }

        Assert.Equal(5m, await StockOfAsync(warehouseId, productId));

        // Disposing the holder rolled its transaction back, which released the lock.
        var accepted = await client.SendAsync(Decrease());

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(4m, await StockOfAsync(warehouseId, productId));
    }

    [Fact]
    public async Task Lock_OfOneOrganization_NeverBlocksAnother()
    {
        await using var holder = CreateContext(TestOrganizationId);
        await using var held = await CreateLock(holder).BeginOrgWriteAsync();

        await using var other = CreateContext(TestOrganizationId + 70_000);
        var stopwatch = Stopwatch.StartNew();
        await using var otherWrite = await CreateLock(other, TestOrganizationId + 70_000).BeginOrgWriteAsync();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Waited {stopwatch.Elapsed} for another organization's lock.");
    }

    [Fact]
    public async Task Lock_IsRefusedWithConflictBusy_AfterTheTimeout()
    {
        await using var holder = CreateContext(TestOrganizationId);
        await using var held = await CreateLock(holder).BeginOrgWriteAsync();

        await using var waiter = CreateContext(TestOrganizationId);
        var refused = await Assert.ThrowsAsync<ConflictException>(() => CreateLock(waiter).BeginOrgWriteAsync());

        Assert.Equal(ErrorCodes.ConflictBusy, refused.Code);
        // The refused write left no transaction open on its connection.
        Assert.Null(waiter.Database.CurrentTransaction);
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }

    private static OrganizationWriteLock CreateLock(ApplicationDbContext context, int organizationId = TestOrganizationId) =>
        new(context, new FakeOrganizationAccessor(organizationId), Options.Create(new WriteLockSettings { TimeoutSeconds = 1 }));
}
