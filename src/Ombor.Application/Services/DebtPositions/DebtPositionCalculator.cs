using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.DebtPositions;

/// <summary>
/// The one place that answers «who owes whom, and how much». A partner's debt is its net ledger balance — the
/// <c>PartnerBalance</c> view behind <c>PartnerDto.Balance</c>: opening balance + unpaid documents ± advances — so a
/// partner holding an advance larger than an unpaid sale owes nothing. The dashboard KPIs, top debtors and aging and
/// the debts summary all come from here, so the same question gives the same number on every page.
/// <para>
/// Past positions (the dashboard trends) are walked back from today's view figures by undoing every event dated at or
/// after the cutoff, so the last point always equals today's headline.
/// </para>
/// </summary>
internal sealed class DebtPositionCalculator(IApplicationDbContext context, IBusinessClock clock)
{
    public async Task<DebtPositionSnapshot> ComputeAsync(IReadOnlyList<DebtCutoff>? cutoffs = null)
    {
        cutoffs ??= [];
        DateTimeOffset? since = cutoffs.Count == 0 ? null : cutoffs.Min(c => c.Before);

        var partners = await LoadPartnersAsync();
        var documents = await LoadDocumentsAsync(since);
        var history = since is { } from ? await LoadHistoryAsync(from, documents) : PartnerHistory.Empty;
        var documentsByPartner = documents.ToLookup(d => d.PartnerId);
        var today = clock.Today;

        var positions = partners
            .Select(p =>
            {
                var own = documentsByPartner[p.Id].ToArray();
                return new PartnerPosition(
                    p.Id, p.Name, p.Company, p.Type, p.IsArchived, p.Balance, p.OpeningBalance, p.PartnerAdvance, p.CompanyAdvance,
                    p.UnpaidReceivable, p.UnpaidPayable,
                    own.Count(d => d.RemainingNow > 0m),
                    DebtAging.Attribute(p.Balance, ReceivableItems(p, own, before: null, history), today, clock.DateOf));
            })
            .ToArray();

        var totals = DebtAging.Totalize(positions.Select(p => (p.Balance, p.AgedReceivable)));
        var atCutoffs = cutoffs
            .Select(cutoff => DebtAging.Totalize(partners.Select(p =>
            {
                var balance = p.Balance - history.EffectsFrom(p.Id, cutoff.Before);
                var items = ReceivableItems(p, documentsByPartner[p.Id], cutoff.Before, history);
                return (balance, DebtAging.Attribute(balance, items, cutoff.AsOf, clock.DateOf));
            })))
            .ToArray();

        return new DebtPositionSnapshot(positions, totals, atCutoffs);
    }

    /// <summary>
    /// The receivable items standing before <paramref name="before"/> (now when null): a positive opening balance, and
    /// every Sale / SupplyRefund dated earlier with something still unpaid at that moment.
    /// </summary>
    private IEnumerable<ReceivableItem> ReceivableItems(
        PartnerRow partner, IEnumerable<DocumentRow> documents, DateTimeOffset? before, PartnerHistory history)
    {
        var openedAt = clock.StartOfDay(partner.OpeningDate);
        if (partner.OpeningBalance > 0m && (before is null || openedAt < before))
        {
            yield return new ReceivableItem(partner.OpeningBalance, openedAt, null);
        }

        foreach (var document in documents.Where(d => d.IsReceivable && (before is null || d.Date < before)))
        {
            var remaining = before is { } cutoff
                ? document.RemainingNow + history.SettledFrom(document.Id, cutoff)
                : document.RemainingNow;

            if (remaining > 0m)
            {
                yield return new ReceivableItem(remaining, document.Date, document.Id);
            }
        }
    }

    private async Task<PartnerRow[]> LoadPartnersAsync()
    {
        // Archived partners stay in: their residual money still counts in totals (rule 31).
        var rows = await context.Partners
            .AsNoTracking()
            .Join(
                context.PartnerBalances,
                partner => partner.Id,
                balance => balance.PartnerId,
                (partner, balance) => new { partner.Id, partner.Name, partner.CompanyName, partner.Type, partner.IsArchived, partner.OpeningDate, Balance = balance })
            .ToArrayAsync();

        return [.. rows.Select(r => new PartnerRow(
            r.Id, r.Name, r.CompanyName, r.Type, r.IsArchived, r.OpeningDate,
            r.Balance.OpeningBalance, r.Balance.Total, r.Balance.PartnerAdvance, r.Balance.CompanyAdvance,
            r.Balance.ReceivableDebt, r.Balance.PayableDebt))];
    }

    /// <summary>
    /// Unpaid documents, plus — when past positions are asked for — every document created or settled since then,
    /// because each was unpaid at some cutoff.
    /// </summary>
    private async Task<DocumentRow[]> LoadDocumentsAsync(DateTimeOffset? since)
    {
        var query = context.Transactions.AsNoTracking();
        query = since is { } from
            ? query.Where(t => t.TotalDue > t.TotalPaid
                || t.DateUtc >= from
                || t.PaymentAllocations.Any(a => a.Type == PaymentAllocationType.TransactionSettlement && a.Payment.DateUtc >= from))
            : query.Where(t => t.TotalDue > t.TotalPaid);

        return await query
            .Select(t => new DocumentRow(t.Id, t.PartnerId, t.Type, t.DateUtc, t.TotalDue, t.TotalDue - t.TotalPaid))
            .ToArrayAsync();
    }

    private async Task<PartnerHistory> LoadHistoryAsync(DateTimeOffset from, DocumentRow[] documents)
    {
        var settlements = await context.PaymentAllocations
            .AsNoTracking()
            .Where(a => a.Type == PaymentAllocationType.TransactionSettlement && a.TransactionId != null && a.Payment.DateUtc >= from)
            .Select(a => new { TransactionId = a.TransactionId!.Value, a.Amount, a.Payment.DateUtc })
            .ToArrayAsync();

        var advanceCredits = await context.PaymentAllocations
            .AsNoTracking()
            .Where(a => a.Type == PaymentAllocationType.AdvanceCredit && a.Payment.PartnerId != null && a.Payment.DateUtc >= from)
            .Select(a => new { PartnerId = a.Payment.PartnerId!.Value, a.Amount, a.Payment.Direction, a.Payment.DateUtc })
            .ToArrayAsync();

        var advanceDraws = await context.PaymentComponents
            .AsNoTracking()
            .Where(c => c.SourceType == PaymentSourceType.Advance && c.Payment.PartnerId != null && c.Payment.DateUtc >= from)
            .Select(c => new { PartnerId = c.Payment.PartnerId!.Value, c.Amount, c.Payment.Direction, c.Payment.DateUtc })
            .ToArrayAsync();

        var openings = await context.Partners
            .AsNoTracking()
            .Select(p => new { p.Id, p.OpeningBalance, p.OpeningDate })
            .ToArrayAsync();

        var history = new PartnerHistory();
        var documentsById = documents.ToDictionary(d => d.Id);

        // Each effect is what the event added to the partner's balance (positive = the partner owes us more), with
        // the same signs as the PartnerBalance view.
        foreach (var document in documents.Where(d => d.Date >= from))
        {
            history.AddEffect(document.PartnerId, document.Date, document.Sign * document.TotalDue);
        }

        foreach (var settlement in settlements)
        {
            if (documentsById.TryGetValue(settlement.TransactionId, out var document))
            {
                history.AddSettlement(document.Id, settlement.DateUtc, settlement.Amount);
                history.AddEffect(document.PartnerId, settlement.DateUtc, -document.Sign * settlement.Amount);
            }
        }

        // An advance the partner pays us is money we hold for them (−); one we pay a supplier is theirs to deliver
        // against (+). Drawing on an advance reverses it.
        foreach (var credit in advanceCredits)
        {
            history.AddEffect(credit.PartnerId, credit.DateUtc, credit.Direction == PaymentDirection.Income ? -credit.Amount : credit.Amount);
        }

        foreach (var draw in advanceDraws)
        {
            history.AddEffect(draw.PartnerId, draw.DateUtc, draw.Direction == PaymentDirection.Income ? draw.Amount : -draw.Amount);
        }

        foreach (var opening in openings)
        {
            var openedAt = clock.StartOfDay(opening.OpeningDate);
            if (openedAt >= from && opening.OpeningBalance != 0m)
            {
                history.AddEffect(opening.Id, openedAt, opening.OpeningBalance);
            }
        }

        return history;
    }

    private sealed record PartnerRow(
        int Id,
        string Name,
        string? Company,
        PartnerType Type,
        bool IsArchived,
        DateOnly OpeningDate,
        decimal OpeningBalance,
        decimal Balance,
        decimal PartnerAdvance,
        decimal CompanyAdvance,
        decimal UnpaidReceivable,
        decimal UnpaidPayable);

    private sealed record DocumentRow(int Id, int PartnerId, TransactionType Type, DateTimeOffset Date, decimal TotalDue, decimal RemainingNow)
    {
        public bool IsReceivable => Type is TransactionType.Sale or TransactionType.SupplyRefund;

        public decimal Sign => IsReceivable ? 1m : -1m;
    }
}
