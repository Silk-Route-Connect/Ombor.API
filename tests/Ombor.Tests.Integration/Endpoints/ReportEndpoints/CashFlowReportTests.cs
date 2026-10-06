using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Report;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ReportEndpoints;

public sealed class CashFlowReportTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ReportTestsBase(factory, output)
{
    [Fact]
    public async Task CashFlow_ForOneWallet_OpensAtTheBalanceBefore_AndClosesOnTheServedBalance()
    {
        // A wallet opened three days ago with 1 000; today a sale pays in 300, a supply pays out 100, 50 moves away.
        var walletId = await CreateWalletAsync(openingBalance: 1_000m, createdAt: BusinessDay.StartOfDay(Today.AddDays(-3)).AddHours(9));
        var otherWalletId = await CreateWalletAsync(openingBalance: 0m);
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10m, unitCost: 20m);
        await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, 3m, 100m, walletId: walletId, paid: 300m));
        await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, 2m, 50m, walletId: walletId, paid: 100m));
        await _client.PostAsync<WalletTransferDto>("wallets/transfers", new CreateWalletTransferRequest(walletId, otherWalletId, 50m, null));

        var today = await GetReportAsync<CashFlowReportDto>("cash-flow", $"{TodayOnly}&walletId={walletId}");
        var balance = (await _client.GetAsync<WalletDto>($"wallets/{walletId}")).Balance;

        var wallet = Assert.Single(today.Wallets);
        Assert.Equal(new CashFlowWalletDto(walletId, wallet.Name, "Cash", false, 1_000m, 0m, 300m, 100m, 0m, 50m, 1_150m), wallet);
        Assert.Equal(balance, wallet.Closing);
        var transactions = Assert.Single(today.ByType, t => t.Type == PaymentType.Transaction);
        Assert.Equal(300m, transactions.Income);
        Assert.Equal(100m, transactions.Expense);
        Assert.Equal(2, transactions.Count);
        Assert.Equal(1_150m, Assert.Single(today.Series).Closing);
        Assert.Equal(wallet.Closing, today.Totals.Closing);

        // Over four days the wallet did not exist at the start: its opening balance arrives on the day it was created.
        var fourDays = await GetReportAsync<CashFlowReportDto>("cash-flow", $"from={Day(Today.AddDays(-3))}&to={Day(Today)}&walletId={walletId}");
        var span = Assert.Single(fourDays.Wallets);
        Assert.Equal(0m, span.Opening);
        Assert.Equal(1_000m, span.InitialBalance);
        Assert.Equal(1_150m, span.Closing);
        Assert.Equal(1_000m, fourDays.Series[0].InitialBalance);
        Assert.Equal(1_000m, fourDays.Series[0].Closing);
        Assert.Equal(1_150m, fourDays.Series[^1].Closing);
    }

    [Fact]
    public async Task CashFlow_ForAllWallets_ClosesOnTheSumOfTheServedBalances()
    {
        var report = await GetReportAsync<CashFlowReportDto>("cash-flow", TodayOnly);
        var wallets = await _client.GetAsync<WalletDto[]>("wallets");

        Assert.Equal(wallets.Length, report.Wallets.Length);
        Assert.Equal(wallets.Sum(w => w.Balance), report.Totals.Closing);
        Assert.Equal(report.Totals.TransfersIn, report.Totals.TransfersOut);
        Assert.Equal(
            report.Totals.Closing,
            report.Totals.Opening + report.Totals.InitialBalance + report.Totals.Income - report.Totals.Expense
            + report.Totals.TransfersIn - report.Totals.TransfersOut);
    }
}
