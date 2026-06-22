using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public sealed class CreatePaymentRecordTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldSettleTransaction_AndBalanceSourceToAllocation()
    {
        // Arrange — a partner owes 10,000 on an open sale.
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 10_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 10_000m)]);

        // Act
        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request);

        // Assert — one wallet source = one settling allocation (rule 8).
        var source = Assert.Single(payment.Sources);
        Assert.Equal("Wallet", source.SourceType);
        Assert.Equal(10_000m, source.Amount);
        var allocation = Assert.Single(payment.Allocations);
        Assert.Equal("TransactionSettlement", allocation.AllocationType);
        Assert.Equal(saleId, allocation.TransactionId);
        Assert.Equal("Sale", allocation.TransactionType); // lets the frontend route a settlement row to the sale detail page
        Assert.Equal(10_000m, allocation.Amount);
        Assert.StartsWith("P-", payment.Number);

        // Assert — the sale is now fully paid.
        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == saleId);
        Assert.Equal(10_000m, sale.TotalPaid);
    }

    [Fact]
    public async Task PostAsync_ShouldParkExcessAsAdvance_WhenNoDebtRemains()
    {
        // Arrange — owes 6,000, pays 10,000 (settles the sale, 4,000 left over).
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 6_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 10_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 6_000m)]);

        // Act
        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request);

        // Assert — settlement + advance, and they sum to the wallet source (rule 8).
        Assert.Contains(payment.Allocations, a => a.AllocationType == "TransactionSettlement" && a.Amount == 6_000m && a.TransactionType == "Sale");
        // Advance isn't tied to a transaction, so it carries no transaction type to route on.
        Assert.Contains(payment.Allocations, a => a.AllocationType == "AdvanceCredit" && a.Amount == 4_000m && a.TransactionType == null);
        Assert.Equal(payment.Sources.Sum(s => s.Amount), payment.Allocations.Sum(a => a.Amount));
    }

    [Fact]
    public async Task PostAsync_ShouldReject_WhenAdvanceWouldLeaveOutstandingDebt()
    {
        // Arrange — owes 10,000 but only settles 6,000 and tries to advance the rest (rule 40).
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 10_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 6_000m)]);

        // Act & Assert
        await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAsync_ShouldReject_WhenSettlementExceedsRemaining()
    {
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 5_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 8_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 8_000m)]);

        await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnNotFound_WhenWalletMissing()
    {
        var partnerId = await CreatePartnerAsync();

        var request = new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, NonExistentEntityId,
            Amount: 1_000m, Description: null, Period: null, Settlements: []);

        await _client.PostAsync<ProblemDetails>(GetUrl(), request, HttpStatusCode.NotFound);
    }
}
