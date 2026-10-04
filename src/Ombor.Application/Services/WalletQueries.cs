using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

/// <summary>
/// The wallet read side: list and detail with the computed balance, the operations timeline and the transfer list.
/// Split from <see cref="WalletService"/>, which keeps the writes.
/// </summary>
internal sealed class WalletQueries(IApplicationDbContext context)
{
    public async Task<WalletDto[]> GetAsync(GetWalletsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Wallets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(w => w.Name.Contains(request.SearchTerm));
        }

        // Archived wallets are intentionally included (rule 31).
        var rows = await query
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(WalletProjection())
            .ToArrayAsync();

        return [.. rows.Select(ToDto)];
    }

    public async Task<WalletDto> GetDtoOrThrowAsync(int id)
    {
        var row = await context.Wallets
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(WalletProjection())
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<Wallet>(id);

        return ToDto(row);
    }

    public async Task<WalletOperationDto[]> GetOperationsAsync(int walletId)
    {
        var openingBalance = await context.Wallets
            .Where(w => w.Id == walletId)
            .Select(w => (decimal?)w.OpeningBalance)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<Wallet>(walletId);

        var transfers = await context.WalletTransfers
            .AsNoTracking()
            .Where(t => t.FromWalletId == walletId || t.ToWalletId == walletId)
            .Select(t => new
            {
                t.Id,
                t.DateUtc,
                t.ToWalletId,
                t.Amount,
                FromName = t.FromWallet.Name,
                ToName = t.ToWallet.Name,
            })
            .ToArrayAsync();

        // Wallet-sourced payment components are the payment side of the ledger (rule 15).
        var payments = await context.PaymentComponents
            .AsNoTracking()
            .Where(c => c.SourceType == Domain.Enums.PaymentSourceType.Wallet && c.WalletId == walletId)
            .Select(c => new
            {
                c.Id,
                c.PaymentId,
                c.Amount,
                c.Payment.DateUtc,
                c.Payment.Number,
                c.Payment.Type,
                c.Payment.Direction,
                c.Payment.PartnerId,
                Party = c.Payment.Partner != null
                    ? c.Payment.Partner.Name
                    : (c.Payment.Employee != null ? c.Payment.Employee.FullName : null),
            })
            .ToArrayAsync();

        var entries = new List<TimelineEntry>(transfers.Length + payments.Length);

        foreach (var t in transfers)
        {
            var isIncoming = t.ToWalletId == walletId;
            entries.Add(new TimelineEntry(t.DateUtc, t.Id, isIncoming ? t.Amount : -t.Amount, new WalletOperationDto(
                Id: t.Id,
                Date: t.DateUtc,
                Kind: "Transfer",
                Direction: isIncoming ? "In" : "Out",
                PaymentNumber: null,
                Party: isIncoming ? t.FromName : t.ToName,
                PartnerId: null,
                Amount: t.Amount,
                BalanceAfter: 0m,
                TransferId: t.Id,
                PaymentId: null)));
        }

        foreach (var p in payments)
        {
            var isIncoming = p.Direction == Domain.Enums.PaymentDirection.Income;
            entries.Add(new TimelineEntry(p.DateUtc, p.Id, isIncoming ? p.Amount : -p.Amount, new WalletOperationDto(
                Id: p.Id,
                Date: p.DateUtc,
                Kind: OperationKind(p.Type, isIncoming),
                Direction: isIncoming ? "In" : "Out",
                PaymentNumber: p.Number?.ToString(),
                Party: p.Party,
                PartnerId: p.PartnerId,
                Amount: p.Amount,
                BalanceAfter: 0m,
                TransferId: null,
                PaymentId: p.PaymentId,
                PaymentType: p.Type.ToString())));
        }

        // Fold the running balance from the opening balance over the merged timeline; it reconciles to the current balance.
        var running = openingBalance;
        var operations = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .Select(e =>
            {
                running += e.SignedAmount;
                return e.Operation with { BalanceAfter = running };
            })
            .ToList();

        operations.Reverse(); // newest-first

        return [.. operations];
    }

    public async Task<WalletTransferDto[]> GetTransfersAsync(int walletId)
    {
        if (!await context.Wallets.AnyAsync(w => w.Id == walletId))
        {
            throw new EntityNotFoundException<Wallet>(walletId);
        }

        var rows = await context.WalletTransfers
            .AsNoTracking()
            .Where(t => t.FromWalletId == walletId || t.ToWalletId == walletId)
            .OrderByDescending(t => t.DateUtc)
            .Select(t => new
            {
                t.Id,
                t.DateUtc,
                t.FromWalletId,
                FromName = t.FromWallet.Name,
                FromType = t.FromWallet.Type,
                t.ToWalletId,
                ToName = t.ToWallet.Name,
                ToType = t.ToWallet.Type,
                t.Amount,
                CreatedBy = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : null,
                t.Note,
            })
            .ToArrayAsync();

        return [.. rows.Select(r => new WalletTransferDto(
            r.Id,
            r.DateUtc,
            r.FromWalletId,
            r.FromName,
            r.FromType.ToString(),
            r.ToWalletId,
            r.ToName,
            r.ToType.ToString(),
            r.Amount,
            r.CreatedBy,
            r.Note))];
    }

    private static string OperationKind(Domain.Enums.PaymentType type, bool isIncoming) => type switch
    {
        Domain.Enums.PaymentType.Deposit => "Deposit",
        Domain.Enums.PaymentType.Withdrawal => "Withdrawal",
        Domain.Enums.PaymentType.Transaction => "Payment",
        Domain.Enums.PaymentType.Payroll => "Expense",
        _ => isIncoming ? "Payment" : "Expense", // General
    };

    private sealed record TimelineEntry(DateTimeOffset Date, int Id, decimal SignedAmount, WalletOperationDto Operation);

    /// <summary>Projects a wallet plus its transfer and payment-component sums — the inputs to the computed balance.</summary>
    private static System.Linq.Expressions.Expression<Func<Wallet, WalletRow>> WalletProjection() =>
        w => new WalletRow(
            w.Id,
            w.Name,
            w.Type,
            w.OpeningBalance,
            w.IsArchived,
            w.CreatedByUser != null ? w.CreatedByUser.FirstName + " " + w.CreatedByUser.LastName : null,
            w.CreatedAt,
            w.IncomingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m,
            w.OutgoingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m,
            // Only Wallet-source components move wallet cash (rule 15); signed by the payment direction.
            w.Components
                .Where(c => c.SourceType == Domain.Enums.PaymentSourceType.Wallet && c.Payment.Direction == Domain.Enums.PaymentDirection.Income)
                .Sum(c => (decimal?)c.Amount) ?? 0m,
            w.Components
                .Where(c => c.SourceType == Domain.Enums.PaymentSourceType.Wallet && c.Payment.Direction == Domain.Enums.PaymentDirection.Expense)
                .Sum(c => (decimal?)c.Amount) ?? 0m,
            // Referenced (rule 32 / DR-20): any payment component or transfer bound to this wallet.
            w.Components.Any() || w.IncomingTransfers.Any() || w.OutgoingTransfers.Any());

    private static decimal Balance(WalletRow row)
        => row.OpeningBalance + row.Incoming - row.Outgoing + row.PaymentsIn - row.PaymentsOut;

    private static WalletDto ToDto(WalletRow row)
    {
        var balance = Balance(row);
        // Advances aren't attributed to wallets until the advance-draw/withdrawal feature lands (rule 11).
        const decimal advancesHeld = 0m;

        return new WalletDto(
            row.Id,
            row.Name,
            row.Type.ToString(),
            balance,
            advancesHeld,
            balance - advancesHeld,
            row.OpeningBalance,
            row.IsArchived,
            row.CreatedBy,
            row.CreatedAt,
            IsDeletable: !row.IsReferenced);
    }

    private sealed record WalletRow(
        int Id,
        string Name,
        Domain.Enums.WalletType Type,
        decimal OpeningBalance,
        bool IsArchived,
        string? CreatedBy,
        DateTimeOffset CreatedAt,
        decimal Incoming,
        decimal Outgoing,
        decimal PaymentsIn,
        decimal PaymentsOut,
        bool IsReferenced);
}
