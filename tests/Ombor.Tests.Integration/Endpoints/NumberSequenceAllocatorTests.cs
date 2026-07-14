using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Services;
using Ombor.Tests.Integration.Helpers;
using Xunit;

namespace Ombor.Tests.Integration.Endpoints;

/// <summary>
/// The document-number allocator is the human handle of the dispute trail (DR-21): numbers must be
/// sequential per organization, isolated across organizations, and collision-free under concurrent creates.
/// Fresh organization ids are used per test so "starts at 1" is deterministic against the shared database.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class NumberSequenceAllocatorTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory _factory;

    public NumberSequenceAllocatorTests(TestingWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task AllocateAsync_StartsAtOne_AndIncrementsMonotonically()
    {
        const int organizationId = 71_001;
        await using var context = CreateContext(organizationId);
        var allocator = new NumberSequenceAllocator(context, new FakeOrganizationAccessor(organizationId));

        var first = await allocator.AllocateAsync(NumberSeriesType.Transaction);
        var second = await allocator.AllocateAsync(NumberSeriesType.Transaction);
        var third = await allocator.AllocateAsync(NumberSeriesType.Transaction);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
    }

    [Fact]
    public async Task AllocateAsync_IsIsolated_AcrossOrganizations()
    {
        const int organizationA = 71_002;
        const int organizationB = 71_003;
        await using var contextA = CreateContext(organizationA);
        await using var contextB = CreateContext(organizationB);
        var allocatorA = new NumberSequenceAllocator(contextA, new FakeOrganizationAccessor(organizationA));
        var allocatorB = new NumberSequenceAllocator(contextB, new FakeOrganizationAccessor(organizationB));

        var a1 = await allocatorA.AllocateAsync(NumberSeriesType.Payment);
        var a2 = await allocatorA.AllocateAsync(NumberSeriesType.Payment);
        var b1 = await allocatorB.AllocateAsync(NumberSeriesType.Payment);

        // Each organization numbers independently from 1 — one organization's activity never advances another's.
        Assert.Equal(1, a1);
        Assert.Equal(2, a2);
        Assert.Equal(1, b1);
    }

    [Fact]
    public async Task AllocateAsync_KeepsSeriesIndependent_WithinOneOrganization()
    {
        const int organizationId = 71_004;
        await using var context = CreateContext(organizationId);
        var allocator = new NumberSequenceAllocator(context, new FakeOrganizationAccessor(organizationId));

        var transaction = await allocator.AllocateAsync(NumberSeriesType.Transaction);
        var payment = await allocator.AllocateAsync(NumberSeriesType.Payment);
        var order = await allocator.AllocateAsync(NumberSeriesType.Order);

        // Three independent counters, each starting at 1.
        Assert.Equal(1, transaction);
        Assert.Equal(1, payment);
        Assert.Equal(1, order);
    }

    [Fact]
    public async Task AllocateAsync_MintsDistinctContiguousNumbers_UnderConcurrency()
    {
        const int organizationId = 71_005;
        const int count = 12;

        // Each allocation runs on its own context/connection so this exercises real row-lock contention.
        var tasks = Enumerable.Range(0, count).Select(async _ =>
        {
            await using var context = CreateContext(organizationId);
            var allocator = new NumberSequenceAllocator(context, new FakeOrganizationAccessor(organizationId));
            return await allocator.AllocateAsync(NumberSeriesType.Order);
        });

        var numbers = await Task.WhenAll(tasks);

        // No duplicates and no gaps: exactly 1..count in some order.
        Assert.Equal(count, numbers.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, count), numbers.OrderBy(n => n));
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
