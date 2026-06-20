using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public sealed class ReadPaymentTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnTheRecordedPayment()
    {
        // Arrange — record a payment.
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 3_000m);
        var created = await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            3_000m, null, null, [new SettlementInput(saleId, 3_000m)]));

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
