using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

/// <summary>DR-20: a wallet referenced by payments/transfers is delete-gated with 409, and IsDeletable agrees.</summary>
public sealed class WalletDeleteGatingTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WalletTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenWalletUnreferenced()
    {
        var walletId = await CreateWalletAsync(0m);

        await _client.DeleteAsync(GetUrl(walletId));

        await _client.GetAsync<ProblemDetails>(GetUrl(walletId), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenWalletHasPayment()
    {
        var walletId = await CreateWalletAsync(10_000m);
        await SeedWalletPaymentAsync(walletId);

        await _client.DeleteAsync<ProblemDetails>(GetUrl(walletId), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_ShouldServeIsDeletable_ReflectingReferences()
    {
        var freeId = await CreateWalletAsync(0m);
        var usedId = await CreateWalletAsync(10_000m);
        await SeedWalletPaymentAsync(usedId);

        var free = await _client.GetAsync<WalletDto>(GetUrl(freeId));
        var used = await _client.GetAsync<WalletDto>(GetUrl(usedId));

        Assert.True(free.IsDeletable);
        Assert.False(used.IsDeletable);
    }

    private async Task SeedWalletPaymentAsync(int walletId)
    {
        var payment = new Payment
        {
            Number = $"P-{Guid.NewGuid():N}",
            Type = PaymentType.General,
            Direction = PaymentDirection.Expense,
            DateUtc = DateTimeOffset.UtcNow,
            WalletId = walletId,
        };
        payment.Components.Add(new PaymentComponent
        {
            Payment = null!,
            SourceType = PaymentSourceType.Wallet,
            WalletId = walletId,
            Amount = 1_000m,
        });
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
    }
}
