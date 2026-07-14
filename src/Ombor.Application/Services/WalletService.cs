using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class WalletService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser) : IWalletService
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

    public async Task<WalletDto> GetByIdAsync(GetWalletByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await BuildDtoOrThrowAsync(request.Id);
    }

    public async Task<WalletDto> CreateAsync(CreateWalletRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: null);

        var entity = new Wallet
        {
            Name = request.Name,
            Type = request.Type.ToDomainType(),
            OpeningBalance = request.OpeningBalance,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = currentUser.UserId,
        };

        context.Wallets.Add(entity);
        await context.SaveChangesAsync();

        return await BuildDtoOrThrowAsync(entity.Id);
    }

    public async Task<WalletDto> UpdateAsync(UpdateWalletRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: entity.Id);

        // Only the name is editable — type and opening balance are immutable (rule 16).
        entity.Name = request.Name;
        await context.SaveChangesAsync();

        return await BuildDtoOrThrowAsync(entity.Id);
    }

    public async Task ArchiveAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = true;

        await context.SaveChangesAsync();
    }

    public async Task RestoreAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = false;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);

        // Reference-gated (DR-20): a wallet with payment or transfer history can't be hard-deleted — the same
        // predicate as the served IsDeletable flag, so the affordance and the guard agree. Archive instead.
        var isReferenced =
            await context.PaymentComponents.AnyAsync(c => c.WalletId == id) ||
            await context.WalletTransfers.AnyAsync(t => t.FromWalletId == id || t.ToWalletId == id);

        if (isReferenced)
        {
            throw new ConflictException("Wallet cannot be deleted because payments or transfers reference it. Archive it instead.");
        }

        context.Wallets.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<WalletTransferDto> CreateTransferAsync(CreateWalletTransferRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var from = await GetOrThrowAsync(request.FromWalletId);
        var to = await GetOrThrowAsync(request.ToWalletId);

        // Hard-block a transfer that would overdraw the source wallet (rule: never below zero).
        var fromBalance = await ComputeBalanceAsync(from.Id);
        if (request.Amount > fromBalance)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.Amount),
                    $"Insufficient balance in wallet '{from.Name}'. Available: {fromBalance}, requested: {request.Amount}."),
            ]);
        }

        // A transfer is a single immutable event row; both wallets' balances derive from it,
        // so the move is atomic by construction (no stored balances to update on either side).
        var transfer = new WalletTransfer
        {
            FromWallet = from,
            ToWallet = to,
            FromWalletId = from.Id,
            ToWalletId = to.Id,
            Amount = request.Amount,
            Note = request.Note,
            DateUtc = DateTimeOffset.UtcNow,
            CreatedById = currentUser.UserId,
        };

        context.WalletTransfers.Add(transfer);
        await context.SaveChangesAsync();

        return new WalletTransferDto(
            transfer.Id,
            transfer.DateUtc,
            from.Id,
            from.Name,
            from.Type.ToString(),
            to.Id,
            to.Name,
            to.Type.ToString(),
            transfer.Amount,
            await ResolveCreatorNameAsync(transfer.CreatedById),
            transfer.Note);
    }

    public async Task<WalletOperationDto[]> GetOperationsAsync(int walletId)
    {
        var wallet = await GetOrThrowAsync(walletId);

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
                PaymentNumber: p.Number,
                Party: p.Party,
                PartnerId: p.PartnerId,
                Amount: p.Amount,
                BalanceAfter: 0m,
                TransferId: null,
                PaymentId: p.PaymentId)));
        }

        // Fold the running balance from the opening balance over the merged timeline; it reconciles to the current balance.
        var running = wallet.OpeningBalance;
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

    private static string OperationKind(Domain.Enums.PaymentType type, bool isIncoming) => type switch
    {
        Domain.Enums.PaymentType.Deposit => "Deposit",
        Domain.Enums.PaymentType.Withdrawal => "Withdrawal",
        Domain.Enums.PaymentType.Transaction => "Payment",
        Domain.Enums.PaymentType.Payroll => "Expense",
        _ => isIncoming ? "Payment" : "Expense", // General
    };

    private sealed record TimelineEntry(DateTimeOffset Date, int Id, decimal SignedAmount, WalletOperationDto Operation);

    public async Task<WalletTransferDto[]> GetTransfersAsync(int walletId)
    {
        await GetOrThrowAsync(walletId);

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

    private async Task<WalletDto> BuildDtoOrThrowAsync(int id)
    {
        var row = await context.Wallets
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(WalletProjection())
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<Wallet>(id);

        return ToDto(row);
    }

    // The transfer overdraft guard shares the one canonical balance formula (WalletCalculationExtensions).
    private Task<decimal> ComputeBalanceAsync(int walletId)
        => context.ComputeWalletBalanceAsync(walletId);

    private async Task EnsureNameIsUniqueAsync(string name, int? excludingId)
    {
        var exists = await context.Wallets
            .AnyAsync(w => w.Name == name && (excludingId == null || w.Id != excludingId));

        if (exists)
        {
            throw new ValidationException(
            [
                new ValidationFailure("Name", $"A wallet named '{name}' already exists."),
            ]);
        }
    }

    private async Task<Wallet> GetOrThrowAsync(int id) =>
        await context.Wallets.FirstOrDefaultAsync(w => w.Id == id)
        ?? throw new EntityNotFoundException<Wallet>(id);

    /// <summary>Resolves a creator's display name (the create path builds its response without re-querying).</summary>
    private async Task<string?> ResolveCreatorNameAsync(int? userId) =>
        userId is int id
            ? await context.Users.Where(u => u.Id == id).Select(u => u.FirstName + " " + u.LastName).FirstOrDefaultAsync()
            : null;

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
