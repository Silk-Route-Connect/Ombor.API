using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

/// <summary>
/// DR-25: a wallet may not be overdrawn by an expense (parity with the negative-stock block, rule 20),
/// and the payment form's wallet balance must reflect real payment activity.
/// </summary>
public sealed class WalletOverdraftTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReject_WhenExpenseOverdrawsWallet()
    {
        var walletId = await CreateWalletAsync(3_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 5_000m, Description: "office supplies", Period: null, Settlements: []);

        await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAsync_ShouldAllow_WhenExpenseWithinBalance()
    {
        var walletId = await CreateWalletAsync(10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 4_000m, Description: "office supplies", Period: null, Settlements: []);

        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());
        Assert.Equal("Expense", payment.Direction);
    }

    [Fact]
    public async Task FormData_WalletBalance_ReflectsPaymentActivity()
    {
        // The form balance used to ignore payments (showed opening) — it must match the real balance now.
        var walletId = await CreateWalletAsync(10_000m);
        await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 4_000m, Description: "office supplies", Period: null, Settlements: []).ToMultipartFormData());

        var form = await _client.GetAsync<PaymentFormDataDto>($"{GetUrl()}/form-data");

        var wallet = Assert.Single(form.Wallets, w => w.Id == walletId);
        Assert.Equal(6_000m, wallet.Balance);
    }
}
