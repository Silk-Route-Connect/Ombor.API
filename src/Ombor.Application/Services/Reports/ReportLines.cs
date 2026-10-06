using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>A document line as the line-based reports read it, dated on the local calendar.</summary>
/// <param name="Amount">The line total; a document's sub-cent rounding residue sits on its first line, so a document's lines add up to its stored total.</param>
/// <param name="Cost">The snapshotted line cost; 0 when unknown.</param>
/// <param name="CostIsEstimated">The cost is an estimate, or unknown.</param>
internal sealed record ReportLine(
    int TransactionId,
    TransactionType Type,
    DateTimeOffset DateUtc,
    DateOnly Date,
    int PartnerId,
    int WarehouseId,
    int ProductId,
    int CategoryId,
    decimal Quantity,
    decimal Amount,
    decimal Cost,
    bool CostIsEstimated)
{
    public bool IsRefund => Type is TransactionType.SaleRefund or TransactionType.SupplyRefund;
}

/// <summary>
/// Loads the lines of sales or purchases (with their refunds) dated in [start, end). Reports read lines, not document
/// totals, so a product or category split adds up to the same totals as a by-day split.
/// </summary>
internal sealed class ReportLines(IApplicationDbContext context, IBusinessClock clock)
{
    public Task<ReportLine[]> SalesAsync(DateTimeOffset start, DateTimeOffset end) =>
        LoadAsync(TransactionType.Sale, TransactionType.SaleRefund, start, end);

    public Task<ReportLine[]> PurchasesAsync(DateTimeOffset start, DateTimeOffset end) =>
        LoadAsync(TransactionType.Supply, TransactionType.SupplyRefund, start, end);

    private async Task<ReportLine[]> LoadAsync(TransactionType document, TransactionType refund, DateTimeOffset start, DateTimeOffset end)
    {
        // The whole line entity is materialized so its Total and Cost are computed by the entity's own rules.
        var rows = await context.TransactionLines
            .AsNoTracking()
            .Where(l => (l.Transaction.Type == document || l.Transaction.Type == refund)
                && l.Transaction.DateUtc >= start && l.Transaction.DateUtc < end)
            .OrderBy(l => l.TransactionId)
            .ThenBy(l => l.Id)
            .Select(l => new
            {
                Line = l,
                l.Transaction.Type,
                l.Transaction.DateUtc,
                l.Transaction.PartnerId,
                l.Transaction.WarehouseId,
                l.Transaction.TotalDue,
                l.Product.CategoryId,
            })
            .ToArrayAsync();

        // The stored document total is rounded to cents while a line total may carry fractions (a percentage discount,
        // a weighed quantity); the residue goes on the first line so every grouping reconciles with the documents list.
        var residues = rows
            .GroupBy(r => r.Line.TransactionId)
            .ToDictionary(g => g.First().Line.Id, g => g.First().TotalDue - g.Sum(r => r.Line.Total));

        return [.. rows.Select(r => new ReportLine(
            r.Line.TransactionId,
            r.Type,
            r.DateUtc,
            clock.DateOf(r.DateUtc),
            r.PartnerId,
            r.WarehouseId,
            r.Line.ProductId,
            r.CategoryId,
            r.Line.Quantity,
            r.Line.Total + residues.GetValueOrDefault(r.Line.Id),
            r.Line.Cost ?? 0m,
            r.Line.CostIsEstimated || r.Line.UnitCost is null))];
    }
}
