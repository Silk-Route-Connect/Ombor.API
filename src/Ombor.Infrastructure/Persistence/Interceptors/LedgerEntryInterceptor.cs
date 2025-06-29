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
        if (eventData.Context is ApplicationDbContext context)
        {
            AddTransactionLedgerEntries(context);
            AddPaymentLedgerEntries(context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddTransactionLedgerEntries(ApplicationDbContext context)
    {
        var ledgerEntries = context.ChangeTracker
            .Entries<TransactionRecord>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => LedgerEntryFactory.FromTransaction(x.Entity))
            .ToList();

        context.LedgerEntries.AddRange(ledgerEntries);
    }

    private static void AddPaymentLedgerEntries(ApplicationDbContext context)
    {
        var paymentLedgerEntries = context.ChangeTracker
            .Entries<Payment>()
            .Where(x => x.State == EntityState.Added && x.Entity.PartnerId != null)
            .Select(x => LedgerEntryFactory.FromPayment(x.Entity))
            .ToList();

        context.LedgerEntries.AddRange(paymentLedgerEntries);
    }
}
