using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Factories;

namespace Ombor.Infrastructure.Persistence.Interceptors;

internal sealed class LedgerEntryInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var partnerBalances = new Dictionary<int, decimal>();
        if (eventData.Context is ApplicationDbContext context)
        {
            AddTransactionLedgerEntries(context, partnerBalances);
            AddPaymentLedgerEntries(context, partnerBalances);
            UpdatePartnerBalances(context, partnerBalances);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddTransactionLedgerEntries(ApplicationDbContext context, Dictionary<int, decimal> partnerBalances)
    {
        var ledgerEntries = context.ChangeTracker
            .Entries<TransactionRecord>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => LedgerEntryFactory.FromTransaction(x.Entity))
            .ToList();

        foreach (var ledgerEntry in ledgerEntries)
        {
            AddPartnerBalance(partnerBalances, ledgerEntry.PartnerId, ledgerEntry.AmountLocal);
        }

        context.LedgerEntries.AddRange(ledgerEntries);
    }

    private static void AddPaymentLedgerEntries(ApplicationDbContext context, Dictionary<int, decimal> partnerBalances)
    {
        var ledgerEntries = context.ChangeTracker
            .Entries<Payment>()
            .Where(x => x.State == EntityState.Added && x.Entity.PartnerId != null)
            .Select(x => LedgerEntryFactory.FromPayment(x.Entity))
            .ToList();

        foreach (var ledgerEntry in ledgerEntries)
        {
            AddPartnerBalance(partnerBalances, ledgerEntry.PartnerId, ledgerEntry.AmountLocal);
        }

        context.LedgerEntries.AddRange(ledgerEntries);
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
