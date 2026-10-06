using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence.DataFixes;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

/// <summary>
/// The stored settlement status must follow the amounts. Seed data once stored part-paid rows as Open (sale №793:
/// 356 000 of 356 702,01 paid read «Не оплачено»); the backfill re-derives the status and leaves correct rows alone.
/// </summary>
public sealed class TransactionStatusBackfillTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    [Fact]
    public async Task Backfill_ShouldRestorePartiallyPaid_ForPartPaidRowStoredAsOpen()
    {
        // Arrange — the №793 shape: almost fully paid, stored as Open.
        var partnerId = await CreatePartnerAsync();
        var transactionId = await PlantAsync(partnerId, totalDue: 356_702.01m, totalPaid: 356_000m, TransactionStatus.Open);

        // Act
        await _context.Database.ExecuteSqlRawAsync(TransactionStatusBackfill.Sql);

        // Assert — stored and served status agree with the amounts.
        var stored = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == transactionId);
        Assert.Equal(TransactionStatus.PartiallyPaid, stored.Status);

        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(transactionId));
        Assert.Equal("PartiallyPaid", detail.Status);
        Assert.Equal(702.01m, detail.Remaining);
    }

    [Theory]
    [InlineData(1_000, 1_000, TransactionStatus.Open, TransactionStatus.Closed)]
    [InlineData(1_000, 0, TransactionStatus.PartiallyPaid, TransactionStatus.Open)]
    [InlineData(1_000, 0, TransactionStatus.Closed, TransactionStatus.Open)]
    [InlineData(0, 0, TransactionStatus.Open, TransactionStatus.Closed)]
    public async Task Backfill_ShouldDeriveStatusFromAmounts(int totalDue, int totalPaid, TransactionStatus stored, TransactionStatus expected)
    {
        var partnerId = await CreatePartnerAsync();
        var transactionId = await PlantAsync(partnerId, totalDue, totalPaid, stored);

        await _context.Database.ExecuteSqlRawAsync(TransactionStatusBackfill.Sql);

        var row = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == transactionId);
        Assert.Equal(expected, row.Status);
        Assert.Equal(expected, TransactionRecord.SettlementStatusOf(totalDue, totalPaid));
    }

    [Fact]
    public async Task Backfill_ShouldBeIdempotent()
    {
        var partnerId = await CreatePartnerAsync();
        await PlantAsync(partnerId, totalDue: 5_000m, totalPaid: 2_000m, TransactionStatus.Open);

        await _context.Database.ExecuteSqlRawAsync(TransactionStatusBackfill.Sql);
        var secondRun = await _context.Database.ExecuteSqlRawAsync(TransactionStatusBackfill.Sql);

        Assert.Equal(0, secondRun);
    }

    private async Task<int> PlantAsync(int partnerId, decimal totalDue, decimal totalPaid, TransactionStatus status)
    {
        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = TransactionType.Sale,
            WarehouseId = await EnsureWarehouseAsync(),
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            Status = status,
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction.Id;
    }
}
