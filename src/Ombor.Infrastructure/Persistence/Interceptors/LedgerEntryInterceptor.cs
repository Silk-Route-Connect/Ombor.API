using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Factories;

namespace Ombor.Infrastructure.Persistence.Interceptors;

internal sealed class LedgerEntryInterceptor : SaveChangesInterceptor
{
    private readonly List<TransactionRecord> _newTransactions = [];
    private readonly List<Payment> _newPayments = [];

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not ApplicationDbContext context)
        {
            return result;
        }

        if (_newTransactions.Count > 0 || _newPayments.Count > 0)
        {
            AddTransactionLedgerEntries(context);
            AddPaymentLedgerEntries(context);
            await context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not ApplicationDbContext context)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var transactions = context.ChangeTracker
                .Entries<TransactionRecord>()
                .Where(x => x.State == EntityState.Added)
                .Select(x => x.Entity);
        var payments = context.ChangeTracker
            .Entries<Payment>()
            .Where(x => x.State == EntityState.Added && x.Entity.PartnerId.HasValue)
            .Select(x => x.Entity);

        var partnerBalances = new Dictionary<int, decimal>();
        foreach (var transaction in transactions)
        {
            var ledgerEntry = LedgerEntryFactory.FromTransaction(transaction);
            AddPartnerBalance(partnerBalances, ledgerEntry.PartnerId, ledgerEntry.AmountLocal);
        }

        foreach (var payment in payments)
        {
            var ledgerEntry = LedgerEntryFactory.FromPayment(payment);
            if (ledgerEntry.Type != Domain.Enums.LedgerType.InvoicePaidUsingBalance)
            {
                AddPartnerBalance(partnerBalances, ledgerEntry.PartnerId, ledgerEntry.AmountLocal);
            }
        }

        UpdatePartnerBalances(context, partnerBalances);

        _newTransactions.AddRange(transactions);
        _newPayments.AddRange(payments);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddTransactionLedgerEntries(ApplicationDbContext context)
    {
        var ledgerEntries = _newTransactions.ConvertAll(LedgerEntryFactory.FromTransaction);
        context.LedgerEntries.AddRange(ledgerEntries);
        _newTransactions.Clear();
    }

    private void AddPaymentLedgerEntries(ApplicationDbContext context)
    {
        var ledgerEntries = _newPayments.ConvertAll(LedgerEntryFactory.FromPayment);
        context.LedgerEntries.AddRange(ledgerEntries);
        _newPayments.Clear();
    }

    private static void UpdatePartnerBalances(ApplicationDbContext context, Dictionary<int, decimal> partnerBalances)
    {
        if (partnerBalances.Count == 0)
        {
            return;
        }

        var partnerIds = partnerBalances.Keys;
        var partners = context.Partners
            .Where(x => partnerIds.Contains(x.Id))
            .AsTracking()
            .ToList();

        foreach (var partner in partners)
        {
            if (partnerBalances.TryGetValue(partner.Id, out var amount))
            {
                partner.Balance += amount;
            }
        }
    }

    private static void AddPartnerBalance(Dictionary<int, decimal> partnerBalances, int partnerId, decimal amount)
    {
        if (amount == 0)
        {
            return;
        }

        if (partnerBalances.TryGetValue(partnerId, out decimal balance))
        {
            partnerBalances[partnerId] = balance + amount;
        }
        else
        {
            partnerBalances.Add(partnerId, amount);
        }
    }
}
