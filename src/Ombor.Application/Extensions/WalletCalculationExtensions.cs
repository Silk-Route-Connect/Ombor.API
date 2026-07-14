using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

/// <summary>
/// One canonical wallet-balance calculation, so every consumer (wallet DTO, payment form, overdraft
/// guards, transfers) agrees and can't drift. Balance = opening + transfers in − transfers out +
/// wallet-source payment income − wallet-source payment expense (only Wallet-source components move
/// wallet cash, signed by the payment direction — rule 15).
/// </summary>
internal static class WalletCalculationExtensions
{
    private static readonly Expression<Func<Wallet, decimal>> BalanceSelector = w =>
        w.OpeningBalance
        + (w.IncomingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m)
        - (w.OutgoingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m)
        + (w.Components
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.Payment.Direction == PaymentDirection.Income)
            .Sum(c => (decimal?)c.Amount) ?? 0m)
        - (w.Components
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.Payment.Direction == PaymentDirection.Expense)
            .Sum(c => (decimal?)c.Amount) ?? 0m);

    public static Task<decimal> ComputeWalletBalanceAsync(this IApplicationDbContext context, int walletId)
        => context.Wallets
            .Where(w => w.Id == walletId)
            .Select(BalanceSelector)
            .FirstAsync();

    /// <summary>
    /// Hard-blocks a wallet debit that would overdraw it (DR-25 — parity with the negative-stock block,
    /// rule 20), mirroring the inter-wallet transfer guard. Surfaces as a 400 with available-vs-requested detail.
    /// </summary>
    public static async Task EnsureWalletCanCoverAsync(this IApplicationDbContext context, int walletId, decimal amount)
    {
        var balance = await context.ComputeWalletBalanceAsync(walletId);

        if (amount > balance)
        {
            var name = await context.Wallets
                .Where(w => w.Id == walletId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync();

            throw new ValidationException(
            [
                new ValidationFailure(
                    "Amount",
                    $"Insufficient balance in wallet '{name}'. Available: {balance}, requested: {amount}."),
            ]);
        }
    }
}
