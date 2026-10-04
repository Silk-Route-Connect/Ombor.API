using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Services.DebtPositions;
using Ombor.Contracts.Responses.Debt;

namespace Ombor.Application.Services;

internal sealed class DebtService(
    IApplicationDbContext context,
    IBusinessClock clock,
    DebtPositionCalculator positions) : IDebtService
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
                t.Number,
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

        var today = clock.Today;

        return [.. rows
            .Select(r => new DebtDto(
                r.Id,
                r.Number?.ToString(),
                r.Type.ToDebtDirection(),
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

    public async Task<DebtSummaryDto> GetSummaryAsync()
    {
        var snapshot = await positions.ComputeAsync();
        var documents = await GetDebtsAsync();
        var totals = snapshot.Totals;

        var receivableDocuments = documents.Where(d => d.Direction == DebtDirections.Receivable).ToArray();
        var payableDocuments = documents.Where(d => d.Direction == DebtDirections.Payable).ToArray();
        var pastDue = documents.Where(d => d.OverdueDays > 0).ToArray();

        var partners = snapshot.Partners
            .Where(p => p.Balance != 0m || p.UnpaidDocumentCount > 0)
            .OrderByDescending(p => Math.Abs(p.Balance))
            .ThenBy(p => p.Name)
            .Select(p => new DebtPartnerPositionDto(
                p.PartnerId,
                p.Name,
                p.Company,
                p.Type.ToString(),
                p.IsArchived,
                DebtDirections.OfBalance(p.Balance),
                p.Balance,
                Math.Abs(p.Balance),
                p.OpeningBalance,
                p.UnpaidReceivable,
                p.UnpaidPayable,
                p.UnpaidDocumentCount,
                p.PartnerAdvance,
                p.CompanyAdvance,
                p.AgedReceivable.Count == 0 ? null : p.AgedReceivable.Max(a => a.AgeDays)))
            .ToArray();

        return new DebtSummaryDto(
            totals.Receivable,
            totals.ReceivablePartners,
            totals.Payable,
            totals.PayablePartners,
            totals.Receivable - totals.Payable,
            totals.OlderThan30Days,
            [.. DebtAging.Buckets.Select((bucket, i) => new DebtAgingBucketDto(bucket, totals.Aging[i]))],
            new DebtDocumentTotalsDto(
                receivableDocuments.Sum(d => d.Remaining),
                receivableDocuments.Length,
                payableDocuments.Sum(d => d.Remaining),
                payableDocuments.Length,
                pastDue.Sum(d => d.Remaining),
                pastDue.Length),
            partners);
    }

    // Whole local days since the document's local date: a sale at 03:00 Tashkent time is 0 days old that day.
    private int AgeDays(DateOnly today, DateTimeOffset date) =>
        Math.Max(0, today.DayNumber - clock.DateOf(date).DayNumber);

    private static int OverdueDays(DateOnly today, DateOnly? dueDate) =>
        dueDate is { } due ? Math.Max(0, today.DayNumber - due.DayNumber) : 0;
}
