using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit;

namespace Ombor.Tests.Integration.Endpoints.OrganizationScoping;

/// <summary>
/// Proves wallets are isolated per organization by the global query filter.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class WalletScopingTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory _factory;

    public WalletScopingTests(TestingWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Wallet_IsVisibleOnlyToOwningOrganization()
    {
        // Arrange — one wallet in organization 1, one in organization 2.
        await using var org1Context = CreateContext(organizationId: 1);
        await using var org2Context = CreateContext(organizationId: 2);

        var org1WalletId = await AddWalletAsync(org1Context);
        var org2WalletId = await AddWalletAsync(org2Context);

        // Act
        var org1Wallets = await org1Context.Wallets.Select(w => w.Id).ToListAsync();
        var org2Wallets = await org2Context.Wallets.Select(w => w.Id).ToListAsync();

        // Assert — each organization sees only its own wallet.
        Assert.Contains(org1WalletId, org1Wallets);
        Assert.DoesNotContain(org2WalletId, org1Wallets);

        Assert.Contains(org2WalletId, org2Wallets);
        Assert.DoesNotContain(org1WalletId, org2Wallets);
    }

    private static async Task<int> AddWalletAsync(ApplicationDbContext context)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();

        return wallet.Id;
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
