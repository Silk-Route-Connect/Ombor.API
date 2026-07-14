using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

/// <summary>
/// Guards on the dispute-grade payment ledger: a payment may only settle its own partner's transactions,
/// duplicate settlement rows can't overpay or crash, and the human payment number stays unique.
/// </summary>
public sealed class PaymentLedgerIntegrityTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReject_WhenSettlingAnotherPartnersTransaction()
    {
        // Arrange — partner B owes on a sale; partner A tries to settle it (would corrupt both ledgers).
        var walletId = await CreateWalletAsync(0m);
        var partnerAId = await CreatePartnerAsync();
        var partnerBId = await CreatePartnerAsync();
        var partnerBSaleId = await CreateOpenSaleAsync(partnerBId, total: 10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerAId, null, walletId,
            Amount: 10_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(partnerBSaleId, 10_000m)]);

        // Act & Assert — a clean 400, and partner B's sale is untouched.
        await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);

        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == partnerBSaleId);
        Assert.Equal(0m, sale.TotalPaid);
    }

    [Fact]
    public async Task PostAsync_ShouldReject_WhenDuplicateRowsOverpayOneTransaction()
    {
        // Arrange — two rows for the same sale summing past its remaining (would 500 on the second apply).
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 12_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 6_000m), new SettlementInput(saleId, 6_000m)]);

        // Act & Assert — a clean 400, never a 500, and the sale is untouched.
        await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);

        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == saleId);
        Assert.Equal(0m, sale.TotalPaid);
    }

    [Fact]
    public async Task PostAsync_ShouldCollapseDuplicateRows_WhenWithinRemaining()
    {
        // Arrange — two rows for the same sale summing to exactly its remaining.
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var saleId = await CreateOpenSaleAsync(partnerId, total: 10_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 10_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 4_000m), new SettlementInput(saleId, 6_000m)]);

        // Act
        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request);

        // Assert — one merged allocation of 10,000 and the sale is paid once, not twice.
        var allocation = Assert.Single(payment.Allocations);
        Assert.Equal("TransactionSettlement", allocation.AllocationType);
        Assert.Equal(saleId, allocation.TransactionId);
        Assert.Equal(10_000m, allocation.Amount);

        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == saleId);
        Assert.Equal(10_000m, sale.TotalPaid);
    }

    [Fact]
    public async Task PostAsync_ShouldMintDistinctNumbers_ForSequentialPayments()
    {
        var walletId = await CreateWalletAsync(0m);
        var partnerId = await CreatePartnerAsync();
        var firstSaleId = await CreateOpenSaleAsync(partnerId, total: 5_000m);
        var secondSaleId = await CreateOpenSaleAsync(partnerId, total: 5_000m);

        var first = await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 5_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(firstSaleId, 5_000m)]));

        var second = await _client.PostAsync<PaymentRecordDto>(GetUrl(), new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 5_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(secondSaleId, 5_000m)]));

        Assert.True(int.TryParse(first.Number, out _)); // bare sequential number, no "P-" prefix
        Assert.True(int.TryParse(second.Number, out _));
        Assert.NotEqual(first.Number, second.Number);
    }
}
