using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class WalletService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    IOrganizationWriteLock writeLock,
    WalletQueries queries) : IWalletService
{
    public Task<WalletDto[]> GetAsync(GetWalletsRequest request) => queries.GetAsync(request);

    public async Task<WalletDto> GetByIdAsync(GetWalletByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await queries.GetDtoOrThrowAsync(request.Id);
    }

    public Task<WalletOperationDto[]> GetOperationsAsync(int walletId) => queries.GetOperationsAsync(walletId);

    public Task<WalletTransferDto[]> GetTransfersAsync(int walletId) => queries.GetTransfersAsync(walletId);

    public async Task<WalletDto> CreateAsync(CreateWalletRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // The opening balance is a money event, so it is written like every other one: under the write lock (which
        // also keeps two parallel creates from passing the name check together).
        await using var write = await writeLock.BeginOrgWriteAsync();

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
        await write.CommitAsync();

        return await queries.GetDtoOrThrowAsync(entity.Id);
    }

    public async Task<WalletDto> UpdateAsync(UpdateWalletRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: entity.Id);

        // Only the name is editable — type and opening balance are immutable (rule 16).
        entity.Name = request.Name;
        await context.SaveChangesAsync();

        return await queries.GetDtoOrThrowAsync(entity.Id);
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

        // The source balance is read under the organization's write lock, so parallel transfers or payments cannot
        // spend the same money twice.
        await using var write = await writeLock.BeginOrgWriteAsync();

        await OwnedReferences.Check()
            .Require(context.Wallets, request.FromWalletId, nameof(request.FromWalletId))
            .Require(context.Wallets, request.ToWalletId, nameof(request.ToWalletId))
            .ThrowIfMissingAsync();

        var from = await GetOrThrowAsync(request.FromWalletId);
        var to = await GetOrThrowAsync(request.ToWalletId);

        // Hard-block a transfer that would overdraw the source wallet (rule: never below zero); the same balance
        // formula as every other overdraft guard (WalletCalculationExtensions).
        var fromBalance = await context.ComputeWalletBalanceAsync(from.Id);
        if (request.Amount > fromBalance)
        {
            throw WalletCalculationExtensions.InsufficientBalance(nameof(request.Amount), from.Name, fromBalance, request.Amount);
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
        await write.CommitAsync();

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
}
