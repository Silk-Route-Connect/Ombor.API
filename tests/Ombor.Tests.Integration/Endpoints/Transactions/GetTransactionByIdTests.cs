using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public sealed class GetTransactionByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    [Fact]
    public async Task GetById_ShouldReturnFullDetail_WithLinesAndSettlingPayments()
    {
        // Arrange — a partially-paid Sale (10,000 due, 4,000 settled through a Cash wallet), one line.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        var (transactionId, walletName) = await SeedSettledSaleAsync(partnerId, warehouseId, productId);

        // Act
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(transactionId));

        // Assert — header (number/direction match the debts read model the row deep-links from)
        Assert.Equal(transactionId, detail.Id);
        Assert.Equal($"S-{transactionId}", detail.Number);
        Assert.Equal("Sale", detail.Type);
        Assert.Equal("Receivable", detail.Direction);
        Assert.Equal("PartiallyPaid", detail.Status);
        Assert.Equal(partnerId, detail.PartnerId);
        Assert.Equal(warehouseId, detail.WarehouseId);
        Assert.Equal(new DateOnly(2026, 7, 1), detail.DueDate);
        Assert.Equal(10_000m, detail.TotalDue);
        Assert.Equal(4_000m, detail.TotalPaid);
        Assert.Equal(6_000m, detail.Remaining);

        // Assert — lines
        var line = Assert.Single(detail.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(10_000m, line.Total);

        // Assert — the embedded settling payment carries the wallet
        var payment = Assert.Single(detail.Payments);
        Assert.Equal(transactionId, payment.TransactionId);
        Assert.Equal(4_000m, payment.Amount);
        Assert.Equal(walletName, payment.WalletName);
        Assert.Equal("Cash", payment.WalletType);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenTransactionMissing()
    {
        await _client.GetAsync<ProblemDetails>(GetUrl(NonExistentEntityId), HttpStatusCode.NotFound);
    }

    private async Task<(int transactionId, string walletName)> SeedSettledSaleAsync(int partnerId, int warehouseId, int productId)
    {
        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = TransactionType.Sale,
            WarehouseId = warehouseId,
            DateUtc = DateTimeOffset.UtcNow,
            DueDate = new DateOnly(2026, 7, 1),
            TotalDue = 10_000m,
            TotalPaid = 4_000m,
            Status = TransactionStatus.PartiallyPaid,
        };
        transaction.Lines.Add(new TransactionLine
        {
            ProductId = productId,
            Product = null!,
            Transaction = null!,
            UnitPrice = 1_000m,
            Discount = 0m,
            DiscountType = DiscountType.Fixed,
            Quantity = 10m,
        });
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

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
            Number = $"P-{Guid.NewGuid():N}",
            Type = PaymentType.Transaction,
            Direction = PaymentDirection.Income,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = partnerId,
            WalletId = wallet.Id,
        };
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = 4_000m,
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            TransactionId = transaction.Id,
            Type = PaymentAllocationType.TransactionSettlement,
            Amount = 4_000m,
        });
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return (transaction.Id, wallet.Name);
    }
}
