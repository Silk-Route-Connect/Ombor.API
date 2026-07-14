using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public sealed class GetTransactionsTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task GetList_ShouldServeProvisionalNumber_MatchingDetail()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var id = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open);

        // Act
        var list = await _client.GetAsync<TransactionDto[]>(GetUrl());
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(id));

        // Assert — the list now serves the document number (was the id fallback) and it matches the detail view.
        var row = Assert.Single(list, t => t.Id == id);
        Assert.Equal((SeededDocumentNumberOffset + id).ToString(), row.Number);
        Assert.Equal(detail.Number, row.Number);
    }

    [Fact]
    public async Task GetList_ShouldComputeOverdue_ForPastDueUnpaid()
    {
        // Arrange — an Open sale five days past due.
        var partnerId = await CreatePartnerAsync();
        var id = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open, dueDate: Today.AddDays(-5));

        // Act
        var list = await _client.GetAsync<TransactionDto[]>(GetUrl());

        // Assert
        Assert.Equal("Overdue", Assert.Single(list, t => t.Id == id).Status);
    }

    [Fact]
    public async Task GetList_ShouldComputeOverdue_ForPastDuePartiallyPaid()
    {
        // Arrange — Overdue overrides PartiallyPaid.
        var partnerId = await CreatePartnerAsync();
        var id = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.PartiallyPaid, dueDate: Today.AddDays(-1));

        var list = await _client.GetAsync<TransactionDto[]>(GetUrl());

        Assert.Equal("Overdue", Assert.Single(list, t => t.Id == id).Status);
    }

    [Fact]
    public async Task GetList_ShouldNotComputeOverdue_OnDueDateBoundaryOrWhenClosed()
    {
        // Arrange — due exactly today is not yet overdue; a Closed row is never overdue even past due.
        var partnerId = await CreatePartnerAsync();
        var dueTodayId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open, dueDate: Today);
        var closedPastDueId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Closed, dueDate: Today.AddDays(-5));

        var list = await _client.GetAsync<TransactionDto[]>(GetUrl());

        Assert.Equal("Open", Assert.Single(list, t => t.Id == dueTodayId).Status);
        Assert.Equal("Closed", Assert.Single(list, t => t.Id == closedPastDueId).Status);
    }

    [Fact]
    public async Task GetList_ShouldServeOriginalTransactionNumber_ForRefundOnly()
    {
        // Arrange — a Sale and a SaleRefund that reverses it.
        var partnerId = await CreatePartnerAsync();
        var saleId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Closed);
        var refundId = await SeedTransactionAsync(partnerId, TransactionType.SaleRefund, TransactionStatus.Open, originalTransactionId: saleId);

        // Act
        var list = await _client.GetAsync<TransactionDto[]>(GetUrl());

        // Assert — the refund carries the original's document number; a non-refund has none.
        var refund = Assert.Single(list, t => t.Id == refundId);
        Assert.Equal(saleId, refund.OriginalTransactionId);
        Assert.Equal((SeededDocumentNumberOffset + saleId).ToString(), refund.OriginalTransactionNumber);
        Assert.Null(Assert.Single(list, t => t.Id == saleId).OriginalTransactionNumber);
    }

    [Fact]
    public async Task GetList_StatusFilter_OverdueSelectsPastDue_AndOpenExcludesThem()
    {
        // Arrange — one past-due Open (shows as Overdue) and one future Open, for an isolated partner.
        var partnerId = await CreatePartnerAsync();
        var overdueId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open, dueDate: Today.AddDays(-3));
        var openId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open, dueDate: Today.AddDays(3));

        // Act
        var overdue = await _client.GetAsync<TransactionDto[]>($"{GetUrl()}?partnerId={partnerId}&status=Overdue");
        var open = await _client.GetAsync<TransactionDto[]>($"{GetUrl()}?partnerId={partnerId}&status=Open");

        // Assert — ?status=Overdue returns the past-due row; ?status=Open excludes it and returns the future-due row.
        Assert.Equal(overdueId, Assert.Single(overdue).Id);
        Assert.Equal("Overdue", overdue[0].Status);
        Assert.Equal(openId, Assert.Single(open).Id);
    }
}
