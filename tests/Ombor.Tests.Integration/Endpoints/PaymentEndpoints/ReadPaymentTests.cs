using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public sealed class ReadPaymentTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task FormData_WalletBalances_MatchTheWalletsList()
    {
        // Arrange — a wallet that moved money only through a payment, and one with an opening balance.
        var paymentOnlyWallet = await CreateWalletAsync(0m);
        var openingWallet = await CreateWalletAsync(7_500m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 2_000m);
        await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, paymentOnlyWallet,
            2_000m, null, null, [new SettlementInput(saleId, 2_000m)]).ToMultipartFormData());

        // Act — balances now come from one batched query.
        var formData = await _client.GetAsync<PaymentFormDataDto>($"{GetUrl()}/form-data");
        var walletList = await _client.GetAsync<Contracts.Responses.Wallet.WalletDto[]>("wallets");

        // Assert — every form wallet shows the same balance as the wallets list.
        Assert.Equal(2_000m, formData.Wallets.Single(w => w.Id == paymentOnlyWallet).Balance);
        Assert.Equal(7_500m, formData.Wallets.Single(w => w.Id == openingWallet).Balance);
        Assert.All(formData.Wallets, w => Assert.Equal(walletList.Single(l => l.Id == w.Id).Balance, w.Balance));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTheRecordedPayment()
    {
        // Arrange — record a payment.
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 3_000m);
        var created = await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            3_000m, null, null, [new SettlementInput(saleId, 3_000m)]).ToMultipartFormData());

        // Act
        var fetched = await _client.GetAsync<PaymentRecordDto>(GetUrl(created.Id));

        // Assert
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(walletId, fetched.WalletId);
        Assert.Single(fetched.Allocations);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenPaymentDoesNotExist()
    {
        var response = await _client.GetAsync<ProblemDetails>(GetUrl(NonExistentEntityId), HttpStatusCode.NotFound);

        response.ShouldBeNotFound<Payment>(NonExistentEntityId);
    }

    [Fact]
    public async Task GetOutstandingAsync_ShouldListOpenTransactions_AndRequirePartnerId()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 7_000m, paid: 2_000m);

        // Act — with partnerId
        var outstanding = await _client.GetAsync<OutstandingTransactionDto[]>($"{GetUrl()}/outstanding?partnerId={partnerId}");

        // Assert — the open sale shows its remaining
        var row = Assert.Single(outstanding, x => x.Id == saleId);
        Assert.Equal(5_000m, row.Remaining);

        // Act & Assert — missing partnerId is a 400
        await _client.GetAsync<ValidationProblemDetails>($"{GetUrl()}/outstanding", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFormDataAsync_ShouldIncludeCreatedWalletAndPartner()
    {
        // Arrange
        var walletId = await CreateWalletAsync(1_000m);
        var partnerId = await CreatePartnerAsync();

        // Act
        var formData = await _client.GetAsync<PaymentFormDataDto>($"{GetUrl()}/form-data");

        // Assert
        Assert.Contains(formData.Wallets, w => w.Id == walletId && w.Balance == 1_000m);
        Assert.Contains(formData.Partners, p => p.Id == partnerId);
    }
}
