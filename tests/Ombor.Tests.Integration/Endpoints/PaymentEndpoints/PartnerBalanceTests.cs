using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

/// <summary>
/// Verifies the partner-balance view reflects the source/allocation payment model: a settlement
/// clears debt and an advance shows the partner is owed money (sign per complexity notes §G).
/// </summary>
public sealed class PartnerBalanceTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task Settlement_ClearsReceivable_AndDepositBecomesPartnerAdvance()
    {
        // Arrange — partner owes 10,000 on an open sale.
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 10_000m);

        // Partner owes us 10,000.
        Assert.Equal(10_000m, await BalanceTotalAsync(partnerId));

        // Act — settle the sale in full.
        await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            10_000m, null, null, [new SettlementInput(saleId, 10_000m)]).ToMultipartFormData());

        // Assert — debt cleared.
        Assert.Equal(0m, await BalanceTotalAsync(partnerId));

        // Act — partner deposits 5,000 with no debt outstanding (becomes an advance).
        await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId,
            5_000m, null, null, []).ToMultipartFormData());

        // Assert — we now owe the partner their 5,000 advance (negative balance).
        Assert.Equal(-5_000m, await BalanceTotalAsync(partnerId));
    }

    private async Task<decimal> BalanceTotalAsync(int partnerId)
    {
        // Read through a fresh context so the SQL view is re-evaluated, not served from the change tracker.
        var balance = await _context.PartnerBalances.AsNoTracking().FirstAsync(b => b.PartnerId == partnerId);

        return balance.Total;
    }
}
