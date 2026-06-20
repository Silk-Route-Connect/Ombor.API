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
            CreatedBy = currentUser.UserId?.ToString(),
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
            CreatedBy = currentUser.UserId?.ToString(),
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
            transfer.CreatedBy,
            transfer.Note);
    }

    public async Task<WalletOperationDto[]> GetOperationsAsync(int walletId)
    {
        var wallet = await GetOrThrowAsync(walletId);

        var transfers = await context.WalletTransfers
            .AsNoTracking()
            .Where(t => t.FromWalletId == walletId || t.ToWalletId == walletId)
            .OrderBy(t => t.DateUtc)
            .ThenBy(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.DateUtc,
                t.FromWalletId,
                t.ToWalletId,
                t.Amount,
                FromName = t.FromWallet.Name,
                ToName = t.ToWallet.Name,
            })
            .ToArrayAsync();

        // Running balance starts at the opening balance and reconciles to the current balance.
        var operations = new List<WalletOperationDto>(transfers.Length);
        var running = wallet.OpeningBalance;

        foreach (var transfer in transfers)
        {
            var isIncoming = transfer.ToWalletId == walletId;
            running += isIncoming ? transfer.Amount : -transfer.Amount;

            operations.Add(new WalletOperationDto(
                Id: transfer.Id,
                Date: transfer.DateUtc,
                Kind: "Transfer",
                Direction: isIncoming ? "In" : "Out",
                PaymentNumber: null,
                Party: isIncoming ? transfer.FromName : transfer.ToName,
                Amount: transfer.Amount,
                BalanceAfter: running,
                TransferId: transfer.Id));
        }

        operations.Reverse(); // newest-first

        return [.. operations];
    }

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
                t.CreatedBy,
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

    private async Task<decimal> ComputeBalanceAsync(int walletId)
    {
        var row = await context.Wallets
            .Where(w => w.Id == walletId)
            .Select(WalletProjection())
            .FirstAsync();

        return row.OpeningBalance + row.Incoming - row.Outgoing;
    }

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

    /// <summary>Projects a wallet plus its transfer sums — the inputs to the computed balance.</summary>
    private static System.Linq.Expressions.Expression<Func<Wallet, WalletRow>> WalletProjection() =>
        w => new WalletRow(
            w.Id,
            w.Name,
            w.Type,
            w.OpeningBalance,
            w.IsArchived,
            w.CreatedBy,
            w.CreatedAt,
            w.IncomingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m,
            w.OutgoingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m);

    private static WalletDto ToDto(WalletRow row)
    {
        var balance = row.OpeningBalance + row.Incoming - row.Outgoing;
        const decimal advancesHeld = 0m; // M2: partner advances physically held in this wallet (rule 11)

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
            row.CreatedAt);
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
        decimal Outgoing);
}
