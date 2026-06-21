using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Debt;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class DebtService(IApplicationDbContext context) : IDebtService
{
    public async Task<DebtDto[]> GetDebtsAsync()
    {
        // Unpaid/partially-paid only. Org scoping is automatic via the global query filter.
        var rows = await context.Transactions
            .AsNoTracking()
            .Where(t => t.TotalDue > t.TotalPaid)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.DateUtc,
                t.DueDate,
                t.TotalDue,
                t.TotalPaid,
                t.PartnerId,
                PartnerName = t.Partner.Name,
                t.Partner.CompanyName,
                PartnerType = t.Partner.Type,
            })
            .ToArrayAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return [.. rows
            .Select(r => new DebtDto(
                r.Id,
                Number(r.Type, r.Id),
                DirectionOf(r.Type),
                r.Type.ToString(),
                r.PartnerId,
                r.PartnerName,
                r.CompanyName,
                r.PartnerType.ToString(),
                r.DateUtc,
                r.DueDate,
                r.TotalDue,
                r.TotalPaid,
                r.TotalDue - r.TotalPaid,
                AgeDays(today, r.DateUtc),
                OverdueDays(today, r.DueDate)))
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.TransactionId)];
    }

    // Receivable = the partner owes us (unpaid Sale / SupplyRefund); Payable = we owe (unpaid Supply / SaleRefund).
    // Same split as View_PartnerBalance, so the totals reconcile (complexity notes §J).
    private static string DirectionOf(TransactionType type) =>
        type is TransactionType.Sale or TransactionType.SupplyRefund ? DebtDirections.Receivable : DebtDirections.Payable;

    // Provisional display number derived from type + id; real per-type sequences are future work (complexity §L).
    private static string Number(TransactionType type, int id)
    {
        var prefix = type switch
        {
            TransactionType.Sale => "S",
            TransactionType.Supply => "SP",
            TransactionType.SaleRefund => "SR",
            TransactionType.SupplyRefund => "SPR",
            _ => "T",
        };

        return $"{prefix}-{id}";
    }

    private static int AgeDays(DateOnly today, DateTimeOffset date) =>
        Math.Max(0, today.DayNumber - DateOnly.FromDateTime(date.UtcDateTime).DayNumber);

    private static int OverdueDays(DateOnly today, DateOnly? dueDate) =>
        dueDate is { } due ? Math.Max(0, today.DayNumber - due.DayNumber) : 0;
}
