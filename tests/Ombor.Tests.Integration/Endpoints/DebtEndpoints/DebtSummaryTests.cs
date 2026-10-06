using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Debt;
using Ombor.Contracts.Responses.Partner;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainPartnerType = Ombor.Domain.Enums.PartnerType;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Tests.Integration.Endpoints.DebtEndpoints;

/// <summary>
/// live-ui-1: «Нам должны» / «Мы должны» is one figure on every page — the net partner balance (opening balance +
/// unpaid documents ± advances). A partner holding an advance larger than an unpaid sale owes nothing; we owe them.
/// </summary>
public sealed class DebtSummaryTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : DebtTestsBase(factory, output)
{
    [Fact]
    public async Task Summary_PartnerWhoseAdvanceExceedsAnUnpaidSale_IsOwedMoney_NotADebtor()
    {
        // Arrange — the София case: a 1 500 advance first, then a 1 000 sale left unpaid.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 50m);
        await _client.PostAsync<PaymentRecordDto>("payments", new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId, 1_500m, null, null, []).ToMultipartFormData());
        var sale = await PostTransactionAsync(TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m));

        // Act
        var summary = await GetSummaryAsync();
        var dashboard = await _client.GetAsync<DashboardDto>(Routes.Dashboard);
        var partner = await _client.GetAsync<PartnerDto>($"{Routes.Partner}/{partnerId}");

        // Assert — net: we owe the partner 500, with the unpaid sale and the advance shown as its parts.
        var position = Assert.Single(summary.Partners, p => p.PartnerId == partnerId);
        Assert.Equal("Payable", position.Direction);
        Assert.Equal(-500m, position.Balance);
        Assert.Equal(500m, position.Amount);
        Assert.Equal(1_000m, position.UnpaidReceivable);
        Assert.Equal(1_500m, position.PartnerAdvance);
        Assert.Null(position.OldestAgeDays);
        Assert.Equal(partner.Balance, position.Balance); // the same figure as the partner page

        // Not a debtor anywhere; the document itself is still listed as unpaid.
        Assert.DoesNotContain(dashboard.TopDebtors, d => d.PartnerId == partnerId);
        Assert.Contains(await GetDebtsAsync(), d => d.TransactionId == sale.Id);
    }

    [Fact]
    public async Task Summary_OpeningBalance_IsDebt_AgedFromTheOpeningDate()
    {
        // Arrange — a partner who owed 3 000 when the business started using Ombor, 45 days ago.
        var partner = new Partner
        {
            Name = $"Partner {Guid.NewGuid():N}",
            Type = DomainPartnerType.Customer,
            OpeningBalance = 3_000m,
            OpeningDate = BusinessDay.Today.AddDays(-45),
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        // Act
        var summary = await GetSummaryAsync();

        // Assert
        var position = Assert.Single(summary.Partners, p => p.PartnerId == partner.Id);
        Assert.Equal("Receivable", position.Direction);
        Assert.Equal(3_000m, position.Amount);
        Assert.Equal(0, position.UnpaidDocumentCount);
        Assert.Equal(45, position.OldestAgeDays);
        Assert.True(summary.OlderThan30Days >= 3_000m);
    }

    [Fact]
    public async Task Summary_AnAdvanceNetsTheOldestDocumentFirst()
    {
        // Arrange — a 1 000 advance, then an old (40 days) and a new unpaid 1 000 sale: the partner owes 1 000.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        await _client.PostAsync<PaymentRecordDto>("payments", new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId, 1_000m, null, null, []).ToMultipartFormData());
        await AddOutstandingTransactionAsync(_context, partnerId, DomainTransactionType.Sale, 1_000m, 0m, DateTimeOffset.UtcNow.AddDays(-40), null);
        await AddOutstandingTransactionAsync(_context, partnerId, DomainTransactionType.Sale, 1_000m, 0m, DateTimeOffset.UtcNow, null);

        // Act
        var position = Assert.Single((await GetSummaryAsync()).Partners, p => p.PartnerId == partnerId);

        // Assert — what stays owed sits on the new sale (the order auto-allocation would settle them).
        Assert.Equal(1_000m, position.Balance);
        Assert.Equal(2_000m, position.UnpaidReceivable);
        Assert.Equal(0, position.OldestAgeDays);
    }

    [Fact]
    public async Task Summary_AnAdvanceWePaid_HasNoAge_AndSitsInNoAgingBucket()
    {
        // Arrange — we prepaid a supplier 800 and nothing was delivered yet: they owe us, but no document dates it.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var before = await GetSummaryAsync();
        await _client.PostAsync<PaymentRecordDto>("payments", new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Expense, partnerId, null, walletId, 800m, null, null, []).ToMultipartFormData());

        // Act
        var summary = await GetSummaryAsync();

        // Assert — a receivable, but not «0 days old»: no age, no bucket, never older than 30 days.
        var position = Assert.Single(summary.Partners, p => p.PartnerId == partnerId);
        Assert.Equal("Receivable", position.Direction);
        Assert.Equal(800m, position.Balance);
        Assert.Equal(800m, position.CompanyAdvance);
        Assert.Null(position.OldestAgeDays);
        Assert.Equal(before.AdvanceReceivable + 800m, summary.AdvanceReceivable);
        Assert.Equal(before.Aging.Select(a => a.Amount), summary.Aging.Select(a => a.Amount));
        Assert.Equal(summary.Receivable, summary.Aging.Sum(a => a.Amount) + summary.AdvanceReceivable);
    }

    [Fact]
    public async Task Summary_ArchivedPartner_StillCounts()
    {
        var partnerId = await CreatePartnerAsync();
        await AddOutstandingTransactionAsync(_context, partnerId, DomainTransactionType.Sale, 700m, 0m, DateTimeOffset.UtcNow, null);
        await _client.PostAsync($"{Routes.Partner}/{partnerId}/archive");

        var position = Assert.Single((await GetSummaryAsync()).Partners, p => p.PartnerId == partnerId);

        Assert.True(position.IsArchived);
        Assert.Equal(700m, position.Amount);
    }

    [Fact]
    public async Task Summary_Totals_EqualTheSumOfPartnerBalances_AndTheDashboard()
    {
        // Arrange — one receivable and one payable partner, so both sides are non-empty.
        var debtor = await CreatePartnerAsync();
        var creditor = await CreatePartnerAsync();
        await AddOutstandingTransactionAsync(_context, debtor, DomainTransactionType.Sale, 1_200m, 200m, DateTimeOffset.UtcNow.AddDays(-50), null);
        await AddOutstandingTransactionAsync(_context, creditor, DomainTransactionType.Supply, 900m, 0m, DateTimeOffset.UtcNow, null);

        // Act
        var summary = await GetSummaryAsync();
        var dashboard = await _client.GetAsync<DashboardDto>(Routes.Dashboard);
        var balances = (await _client.GetAsync<PartnerDto[]>(Routes.Partner))
            .Concat(await _client.GetAsync<PartnerDto[]>($"{Routes.Partner}?isArchived=true"))
            .Select(p => p.Balance)
            .ToArray();

        // Assert — the partner pages, the debts summary and the dashboard give the same numbers.
        Assert.Equal(balances.Where(b => b > 0m).Sum(), summary.Receivable);
        Assert.Equal(-balances.Where(b => b < 0m).Sum(), summary.Payable);
        Assert.Equal(balances.Count(b => b > 0m), summary.ReceivablePartnerCount);
        Assert.Equal(summary.Receivable - summary.Payable, summary.Net);
        Assert.Equal(summary.Receivable, summary.Aging.Sum(a => a.Amount) + summary.AdvanceReceivable);

        Assert.Equal(summary.Receivable, dashboard.Receivable.Value);
        Assert.Equal(summary.ReceivablePartnerCount, dashboard.Receivable.Count);
        Assert.Equal(summary.Payable, dashboard.Payable.Value);
        Assert.Equal(summary.OlderThan30Days, dashboard.Overdue.Value);
        Assert.Equal(summary.Aging.Select(a => a.Amount), dashboard.Aging.Select(a => a.Amount));
        Assert.All(dashboard.TopDebtors, d => Assert.Equal(summary.Partners.Single(p => p.PartnerId == d.PartnerId).Balance, d.Amount));

        // The document totals are the /debts list's own sums.
        var documents = await GetDebtsAsync();
        Assert.Equal(documents.Where(d => d.Direction == "Receivable").Sum(d => d.Remaining), summary.UnpaidDocuments.Receivable);
        Assert.Equal(documents.Count(d => d.Direction == "Payable"), summary.UnpaidDocuments.PayableCount);
    }

    [Fact]
    public async Task Debts_AgeDays_FollowTheLocalDay()
    {
        // A sale one minute after local midnight is 0 days old all that local day (UTC still says "yesterday").
        var partnerId = await CreatePartnerAsync();
        var id = await AddOutstandingTransactionAsync(
            _context, partnerId, DomainTransactionType.Sale, 500m, 0m, BusinessDay.StartOfDay(BusinessDay.Today).AddMinutes(1), null);

        var debt = Assert.Single(await GetDebtsAsync(), d => d.TransactionId == id);

        Assert.Equal(0, debt.AgeDays);
    }

    private Task<DebtSummaryDto> GetSummaryAsync() => _client.GetAsync<DebtSummaryDto>($"{Routes.Debt}/summary");
}
