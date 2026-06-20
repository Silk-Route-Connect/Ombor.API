using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

public abstract class WalletTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected override string GetUrl()
        => Routes.Wallet;

    protected override string GetUrl(int id)
        => $"{Routes.Wallet}/{id}";

    protected async Task<int> CreateWalletAsync(decimal openingBalance, WalletType type = WalletType.Cash, string? name = null)
    {
        var wallet = new Wallet
        {
            Name = name ?? $"Wallet {Guid.NewGuid():N}",
            Type = type,
            OpeningBalance = openingBalance,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }
}
