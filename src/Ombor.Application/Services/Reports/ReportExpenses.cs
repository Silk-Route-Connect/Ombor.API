using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>An expense payment dated on the local calendar, with the money it took out of wallets.</summary>
internal sealed record ReportExpense(
    int PaymentId,
    PaymentType Type,
    DateOnly Date,
    int? PartnerId,
    int? EmployeeId,
    string? Notes,
    decimal Amount);

/// <summary>
/// Loads expense payments dated in [start, end). The amount is what left wallets (wallet-sourced components): a
/// settlement drawn from an advance moved no money in the period, so the expenses report agrees with the cash flow.
/// </summary>
internal sealed class ReportExpenses(IApplicationDbContext context, IBusinessClock clock)
{
    public async Task<ReportExpense[]> LoadAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var components = await context.PaymentComponents
            .AsNoTracking()
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null
                && c.Payment.Direction == PaymentDirection.Expense
                && c.Payment.DateUtc >= start && c.Payment.DateUtc < end)
            .Select(c => new
            {
                c.PaymentId,
                c.Amount,
                c.Payment.Type,
                c.Payment.DateUtc,
                c.Payment.PartnerId,
                c.Payment.EmployeeId,
                c.Payment.Notes,
            })
            .ToArrayAsync();

        return [.. components
            .GroupBy(c => c.PaymentId)
            .Select(g =>
            {
                var payment = g.First();

                return new ReportExpense(
                    g.Key,
                    payment.Type,
                    clock.DateOf(payment.DateUtc),
                    payment.PartnerId,
                    payment.EmployeeId,
                    payment.Notes,
                    g.Sum(c => c.Amount));
            })];
    }
}
