using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public sealed class PartnerLedgerTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PartnerTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task Ledger_ShouldReconcileToBalance_AcrossOpeningSaleAndPayment()
    {
        // Arrange — opening 2,000; a 10,000 sale paid 4,000.
        var partnerId = await CreateLedgerPartnerAsync(openingBalance: 2_000m);
        var saleId = await CreateOpenTransactionAsync(partnerId, TransactionType.Sale, due: 10_000m, paid: 4_000m);
        var walletName = await CreateSettlementPaymentAsync(partnerId, saleId, PaymentDirection.Income, amount: 4_000m);

        // Act
        var partner = await _client.GetAsync<PartnerDto>(GetUrl(partnerId));
        var ledger = await _client.GetAsync<PartnerLedgerEntryDto[]>($"{GetUrl(partnerId)}/ledger");

        // Assert — balance = opening 2,000 + unpaid 6,000 = 8,000.
        Assert.Equal(8_000m, partner.Balance);
        Assert.Equal(2_000m, partner.OpeningBalance);

        // Newest-first; the latest running balance reconciles to the partner balance.
        Assert.Equal(3, ledger.Length);
        Assert.Equal(partner.Balance, ledger[0].Balance);
        Assert.Equal("opening", ledger[^1].Type);
        Assert.Equal(2_000m, ledger[^1].Balance);

        var sale = ledger.Single(e => e.Type == "sale");
        Assert.Equal(10_000m, sale.Delta);
        Assert.Equal("partial", sale.Status);
        Assert.Null(sale.WalletName); // a transaction isn't tied to a single wallet

        var payment = ledger.Single(e => e.Type == "payment");
        Assert.Equal(-4_000m, payment.Delta); // an Income payment reduces what the partner owes
        Assert.Equal(walletName, payment.WalletName); // payment rows carry the wallet for the «Платежи» tab column
        Assert.Equal("Cash", payment.WalletType);

        Assert.Null(ledger.Single(e => e.Type == "opening").WalletName);
    }

    [Fact]
    public async Task Ledger_ShouldExposeTransactionNumber_AsReferenceOnTransactionRows()
    {
        // Arrange — a sale carrying a persisted document number (F20). Opening balance 0 keeps this focused.
        var partnerId = await CreateLedgerPartnerAsync(openingBalance: 0m);
        await CreateOpenTransactionAsync(partnerId, TransactionType.Sale, due: 5_000m, paid: 0m, number: 797);

        // Act
        var ledger = await _client.GetAsync<PartnerLedgerEntryDto[]>($"{GetUrl(partnerId)}/ledger");

        // Assert — the sale row serves the bare transaction number; the opening row has no reference.
        var sale = ledger.Single(e => e.Type == "sale");
        Assert.Equal("797", sale.Reference);
        Assert.Null(ledger.Single(e => e.Type == "opening").Reference);
    }

    [Fact]
    public async Task Delete_ShouldReturnConflict_WhenPartnerIsReferenced()
    {
        // Arrange — a partner with a transaction can't be hard-deleted.
        var partnerId = await CreateLedgerPartnerAsync(openingBalance: 0m);
        await CreateOpenTransactionAsync(partnerId, TransactionType.Sale, due: 1_000m, paid: 0m);

        // Act & Assert
        await _client.DeleteAsync<ProblemDetails>(GetUrl(partnerId), HttpStatusCode.Conflict);

        var partner = await _client.GetAsync<PartnerDto>(GetUrl(partnerId));
        Assert.False(partner.IsDeletable);
        Assert.True(partner.ActivityCount >= 1);
    }

    [Fact]
    public async Task Delete_ShouldSucceed_WhenPartnerIsNotReferenced()
    {
        var partnerId = await CreateLedgerPartnerAsync(openingBalance: 0m);

        await _client.DeleteAsync(GetUrl(partnerId), HttpStatusCode.NoContent);
    }

    private async Task<int> CreateLedgerPartnerAsync(decimal openingBalance)
    {
        var partner = new Partner
        {
            Name = $"Ledger Partner {Guid.NewGuid():N}",
            Type = PartnerType.Both,
            OpeningBalance = openingBalance,
            OpeningDate = new DateOnly(2026, 1, 1),
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    private async Task<int> CreateOpenTransactionAsync(int partnerId, TransactionType type, decimal due, decimal paid, int? number = null)
    {
        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            Number = number,
            WarehouseId = await EnsureWarehouseAsync(),
            DateUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            TotalDue = due,
            TotalPaid = paid,
            Status = paid >= due ? TransactionStatus.Closed : (paid > 0 ? TransactionStatus.PartiallyPaid : TransactionStatus.Open),
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction.Id;
    }

    private async Task<string> CreateSettlementPaymentAsync(int partnerId, int transactionId, PaymentDirection direction, decimal amount)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            Number = null,
            Type = PaymentType.Transaction,
            Direction = direction,
            DateUtc = new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero),
            PartnerId = partnerId,
            WalletId = wallet.Id,
        };
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = amount,
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            TransactionId = transactionId,
            Type = PaymentAllocationType.TransactionSettlement,
            Amount = amount,
        });

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return wallet.Name;
    }
}
