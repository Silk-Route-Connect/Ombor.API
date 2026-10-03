using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Validators;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

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
    /// rule 20), mirroring the inter-wallet transfer guard. Surfaces as a 400 <c>wallet.insufficient_balance</c> on
    /// <paramref name="propertyName"/> with the wallet name and available-vs-requested params.
    /// </summary>
    public static async Task EnsureWalletCanCoverAsync(
        this IApplicationDbContext context,
        int walletId,
        decimal amount,
        string propertyName = "Amount")
    {
        var balance = await context.ComputeWalletBalanceAsync(walletId);

        if (amount > balance)
        {
            var name = await context.Wallets
                .Where(w => w.Id == walletId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync();

            throw InsufficientBalance(propertyName, name, balance, amount);
        }
    }

    /// <summary>The coded overdraft 400 shared by payments, payroll, transaction payments and wallet transfers.</summary>
    public static ValidationException InsufficientBalance(string propertyName, string? walletName, decimal available, decimal requested) =>
        CodedValidation.Failure(
            propertyName,
            $"Insufficient balance in wallet '{walletName}'. Available: {available}, requested: {requested}.",
            ErrorCodes.WalletInsufficientBalance,
            new Dictionary<string, object?>
            {
                ["walletName"] = walletName,
                ["available"] = available,
                ["requested"] = requested,
            });
}
