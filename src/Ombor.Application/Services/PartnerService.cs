using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PartnerService(IApplicationDbContext context, IRequestValidator validator) : IPartnerService
{
    public async Task<PartnerDto[]> GetAsync(GetPartnersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Partners
            .Where(x => x.IsArchived == (request.IsArchived ?? false));

        var searchTerm = request.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Address != null && x.Address.Contains(searchTerm)) ||
                (x.Email != null && x.Email.Contains(searchTerm)) ||
                (x.CompanyName != null && x.CompanyName.Contains(searchTerm)));
        }

        return await ProjectAsync(query.OrderBy(x => x.Name));
    }

    public async Task<PartnerDto> GetByIdAsync(GetPartnerByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var dtos = await ProjectAsync(context.Partners.Where(x => x.Id == request.Id));

        return dtos.FirstOrDefault() ?? throw new EntityNotFoundException<Partner>(request.Id);
    }

    public async Task<CreatePartnerResponse> CreateAsync(CreatePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        // Opening balance is an immutable event stamped at creation.
        entity.OpeningDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        context.Partners.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdatePartnerResponse> UpdateAsync(UpdatePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeletePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        var isReferenced =
            await context.Transactions.AnyAsync(x => x.PartnerId == entity.Id) ||
            await context.Payments.AnyAsync(x => x.PartnerId == entity.Id) ||
            await context.Orders.AnyAsync(x => x.CustomerId == entity.Id) ||
            await context.Templates.AnyAsync(x => x.PartnerId == entity.Id);

        // Referenced partners can't be hard-deleted (rule: 409, the UI steers to archive).
        if (isReferenced)
        {
            throw new ConflictException(
                "Partner cannot be deleted because other records reference it. Archive it instead.");
        }

        context.Partners.Remove(entity);
        await context.SaveChangesAsync();
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

    public async Task<PartnerLedgerEntryDto[]> GetLedgerAsync(int partnerId)
    {
        var partner = await context.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new EntityNotFoundException<Partner>(partnerId);

        var transactions = await context.Transactions
            .Where(t => t.PartnerId == partnerId)
            .Select(t => new { t.Id, t.DateUtc, t.Number, t.Type, t.TotalDue, t.TotalPaid, ItemCount = t.Lines.Count })
            .ToArrayAsync();

        var payments = await context.Payments
            .Where(p => p.PartnerId == partnerId)
            .Select(p => new
            {
                p.Id,
                p.DateUtc,
                p.Number,
                p.Type,
                p.Direction,
                WalletName = p.Wallet != null ? p.Wallet.Name : null,
                WalletType = p.Wallet != null ? p.Wallet.Type.ToString() : null,
                // Only settling allocations move the balance; ChangeReturn is a memo (rule 10).
                Settling = p.Allocations
                    .Where(a => a.Type == PaymentAllocationType.TransactionSettlement || a.Type == PaymentAllocationType.AdvanceCredit)
                    .Sum(a => (decimal?)a.Amount) ?? 0m,
            })
            .ToArrayAsync();

        var events = new List<LedgerEvent>(transactions.Length + payments.Length + 1)
        {
            new(
                0,
                "opening",
                new DateTimeOffset(partner.OpeningDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                partner.OpeningBalance,
                null, // SourceId — opening has no underlying record
                null,
                null,
                null,
                null, // WalletName — opening has no wallet
                null),
        };

        foreach (var t in transactions)
        {
            var (type, sign) = t.Type switch
            {
                TransactionType.Sale => ("sale", 1),
                TransactionType.SupplyRefund => ("refund-supply", 1),
                TransactionType.Supply => ("supply", -1),
                TransactionType.SaleRefund => ("refund-sale", -1),
                _ => ("sale", 1),
            };

            var status = t.TotalPaid <= 0m
                ? "unpaid"
                : (t.TotalPaid < t.TotalDue ? "partial" : "paid");

            // Transactions aren't tied to a single wallet (settled across zero-to-many payments), so no wallet here.
            // Reference carries the bare document Number (null on synthetic seed rows), mirroring payments below.
            events.Add(new(t.Id, type, t.DateUtc, sign * t.TotalDue, t.Id, t.Number?.ToString(), t.ItemCount, status, null, null));
        }

        foreach (var p in payments)
        {
            // Income reduces what the partner owes (−); Expense reduces what we owe (+).
            var sign = p.Direction == PaymentDirection.Income ? -1 : 1;
            var type = p.Type switch
            {
                PaymentType.Deposit => "deposit",
                PaymentType.Withdrawal => "withdraw",
                _ => "payment",
            };

            events.Add(new(p.Id, type, p.DateUtc, sign * p.Settling, p.Id, p.Number?.ToString(), null, "done", p.WalletName, p.WalletType));
        }

        // Fold the running balance oldest→newest (final value reconciles to PartnerBalance.Total), then newest-first.
        var running = 0m;
        var ordered = events
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .Select(e =>
            {
                running += e.Delta;
                return new PartnerLedgerEntryDto(e.Id, e.Type, e.Date, e.Delta, running, e.SourceId, e.Reference, e.ItemCount, e.Status, e.WalletName, e.WalletType);
            })
            .ToList();

        ordered.Reverse();

        return [.. ordered];
    }

    private async Task<PartnerDto[]> ProjectAsync(IQueryable<Partner> query)
    {
        var rows = await query
            .AsNoTracking()
            .Join(
                context.PartnerBalances,
                partner => partner.Id,
                balance => balance.PartnerId,
                (partner, balance) => new { partner, balance })
            .Select(x => new
            {
                x.partner.Id,
                x.partner.Name,
                x.partner.Type,
                x.partner.Address,
                x.partner.Email,
                x.partner.CompanyName,
                x.partner.PhoneNumbers,
                x.partner.OpeningBalance,
                x.partner.OpeningDate,
                x.partner.IsArchived,
                x.balance.PartnerAdvance,
                x.balance.CompanyAdvance,
                x.balance.PayableDebt,
                x.balance.ReceivableDebt,
                ActivityCount = x.partner.Transactions.Count + x.partner.Payments.Count + x.partner.Orders.Count + x.partner.Templates.Count,
            })
            .ToArrayAsync();

        return [.. rows.Select(r => new PartnerDto(
            r.Id,
            r.Name,
            r.Type.ToString(),
            r.Address,
            r.Email,
            r.CompanyName,
            r.PhoneNumbers,
            r.OpeningBalance + (r.ReceivableDebt + r.CompanyAdvance) - (r.PayableDebt + r.PartnerAdvance),
            r.OpeningBalance,
            r.OpeningDate,
            r.IsArchived,
            r.ActivityCount == 0,
            r.ActivityCount))];
    }

    private async Task<Partner> GetOrThrowAsync(int id) =>
        await context.Partners.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Partner>(id);

    private sealed record LedgerEvent(
        int Id,
        string Type,
        DateTimeOffset Date,
        decimal Delta,
        int? SourceId,
        string? Reference,
        int? ItemCount,
        string? Status,
        string? WalletName,
        string? WalletType);
}
