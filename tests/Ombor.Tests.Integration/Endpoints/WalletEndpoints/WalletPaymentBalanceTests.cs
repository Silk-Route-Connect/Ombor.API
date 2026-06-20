using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using PartnerType = Ombor.Domain.Enums.PartnerType;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

public sealed class WalletPaymentBalanceTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WalletTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task Balance_ShouldRise_WhenWalletReceivesAnIncomePayment()
    {
        // Arrange
        var walletId = await CreateWalletAsync(openingBalance: 0m);
        var partnerId = await CreatePartnerAsync();

        // Act — a deposit moves money into the wallet (Income).
        var request = new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 5_000m, Description: null, Period: null, Settlements: []);
        await _client.PostAsync<PaymentRecordDto>("payments", request);

        // Assert — balance includes the wallet-sourced component; advances aren't attributed yet.
        var wallet = await _client.GetAsync<WalletDto>(GetUrl(walletId));
        Assert.Equal(5_000m, wallet.Balance);
        Assert.Equal(0m, wallet.AdvancesHeld);
        Assert.Equal(wallet.Balance, wallet.OurMoney);

        // Assert — the ledger shows the payment row reconciling to the balance.
        var ops = await _client.GetAsync<WalletOperationDto[]>($"{GetUrl(walletId)}/operations");
        var op = Assert.Single(ops);
        Assert.Equal("Deposit", op.Kind);
        Assert.Equal("In", op.Direction);
        Assert.Equal(5_000m, op.Amount);
        Assert.Equal(5_000m, op.BalanceAfter);
    }

    [Fact]
    public async Task Balance_ShouldFall_WhenWalletPaysAnExpense()
    {
        // Arrange
        var walletId = await CreateWalletAsync(openingBalance: 10_000m);

        // Act — a general expense moves money out of the wallet.
        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 4_000m, Description: "office supplies", Period: null, Settlements: []);
        await _client.PostAsync<PaymentRecordDto>("payments", request);

        // Assert
        var wallet = await _client.GetAsync<WalletDto>(GetUrl(walletId));
        Assert.Equal(6_000m, wallet.Balance);
        Assert.Equal(wallet.Balance, wallet.OurMoney);

        var ops = await _client.GetAsync<WalletOperationDto[]>($"{GetUrl(walletId)}/operations");
        var op = Assert.Single(ops);
        Assert.Equal("Expense", op.Kind);
        Assert.Equal("Out", op.Direction);
        Assert.Equal(4_000m, op.Amount);
        Assert.Equal(6_000m, op.BalanceAfter);
    }

    [Fact]
    public async Task Operations_ShouldMergePaymentsAndTransfers_NewestFirst_ReconcilingToBalance()
    {
        // Arrange — opening 1,000; deposit 500 in; transfer 200 out.
        var walletId = await CreateWalletAsync(openingBalance: 1_000m);
        var otherWalletId = await CreateWalletAsync(openingBalance: 0m);
        var partnerId = await CreatePartnerAsync();

        await _client.PostAsync<PaymentRecordDto>("payments", new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 500m, Description: null, Period: null, Settlements: []));

        await _client.PostAsync<WalletTransferDto>(
            $"{GetUrl()}/transfers",
            new CreateWalletTransferRequest(walletId, otherWalletId, 200m, null));

        // Assert — balance = 1000 + 500 − 200 = 1300, and the newest op reconciles to it.
        var wallet = await _client.GetAsync<WalletDto>(GetUrl(walletId));
        Assert.Equal(1_300m, wallet.Balance);

        var ops = await _client.GetAsync<WalletOperationDto[]>($"{GetUrl(walletId)}/operations");
        Assert.Equal(2, ops.Length);
        Assert.Equal("Transfer", ops[0].Kind); // newest first — the transfer happened last
        Assert.Equal(1_300m, ops[0].BalanceAfter);
        Assert.Equal(1_500m, ops[1].BalanceAfter); // after the deposit, before the transfer
    }

    private async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner
        {
            Name = $"Partner {Guid.NewGuid():N}",
            Type = PartnerType.Both,
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }
}
