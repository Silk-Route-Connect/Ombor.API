using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

/// <summary>
/// The read side of <see cref="IPaymentService"/>: payment records, the payment-form data, a partner's outstanding
/// transactions, and a transaction's settling payments. Split from <see cref="PaymentService"/>, which keeps the
/// writes and delegates here.
/// </summary>
internal sealed class PaymentQueries(IApplicationDbContext context)
{
    public Task<PaymentRecordDto[]> GetRecordsAsync(GetPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return RecordsQuery(request)
            .OrderByDescending(p => p.DateUtc)
            .Select(ToRecordExpression())
            .ToArrayAsync();
    }

    public async Task<PaymentRecordDto> GetRecordByIdAsync(int id)
    {
        var record = await RecordsQuery(new GetPaymentsRequest())
            .Where(p => p.Id == id)
            .Select(ToRecordExpression())
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<Payment>(id);

        return record;
    }

    public async Task<PaymentFormDataDto> GetFormDataAsync()
    {
        var partners = await context.Partners
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Name)
            .Join(
                context.PartnerBalances,
                partner => partner.Id,
                balance => balance.PartnerId,
                (partner, balance) => new PaymentFormPartnerDto(
                    partner.Id,
                    partner.Name,
                    partner.Type.ToString(),
                    balance.Total,
                    balance.PartnerAdvance))
            .ToArrayAsync();

        var employees = await context.Employees
            .OrderBy(e => e.FullName)
            .Select(e => new PaymentFormEmployeeDto(e.Id, e.FullName, e.Position, e.Salary))
            .ToArrayAsync();

        var walletRows = await context.Wallets
            .Where(w => !w.IsArchived)
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, Type = w.Type.ToString() })
            .ToArrayAsync();

        var wallets = new PaymentFormWalletDto[walletRows.Length];
        for (var i = 0; i < walletRows.Length; i++)
        {
            var w = walletRows[i];
            // Full computed balance incl. payment activity, via the shared calculator — the form must
            // match the wallets list (a payment-only wallet used to show 0 here while the list showed its
            // real, possibly negative, balance).
            var balance = await context.ComputeWalletBalanceAsync(w.Id);
            wallets[i] = new PaymentFormWalletDto(w.Id, w.Name, w.Type, balance);
        }

        return new PaymentFormDataDto(partners, employees, wallets);
    }

    public Task<OutstandingTransactionDto[]> GetOutstandingAsync(int partnerId)
    {
        return context.Transactions
            .Where(t => t.PartnerId == partnerId && t.TotalDue > t.TotalPaid)
            .OrderBy(t => t.DateUtc)
            .Select(t => new OutstandingTransactionDto(
                t.Id,
                t.DateUtc,
                t.Type.ToString(),
                t.TotalDue,
                t.TotalPaid,
                t.TotalDue - t.TotalPaid))
            .ToArrayAsync();
    }

    private IQueryable<Payment> RecordsQuery(GetPaymentsRequest request)
    {
        var query = context.Payments
            .Include(p => p.Partner)
            .Include(p => p.Employee)
            .Include(p => p.Wallet)
            .Include(p => p.Components).ThenInclude(c => c.Wallet)
            .Include(p => p.Allocations)
            .AsNoTracking()
            .AsQueryable();

        if (request.PartnerId.HasValue)
        {
            query = query.Where(p => p.PartnerId == request.PartnerId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == request.EmployeeId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(p => p.DateUtc >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(p => p.DateUtc <= request.ToDate.Value);
        }

        if (request.Type.HasValue)
        {
            var type = request.Type.Value.ToDomainType();
            query = query.Where(p => p.Type == type);
        }

        if (request.Direction.HasValue)
        {
            var direction = request.Direction.Value.ToDomainDirection();
            query = query.Where(p => p.Direction == direction);
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<Payment, PaymentRecordDto>> ToRecordExpression() =>
        p => new PaymentRecordDto(
            p.Id,
            p.Number.ToString(),
            p.DateUtc,
            p.Type.ToString(),
            p.Direction.ToString(),
            p.PartnerId,
            p.Partner != null ? p.Partner.Name : null,
            p.Partner != null ? p.Partner.Type.ToString() : null,
            p.EmployeeId,
            p.Employee != null ? p.Employee.FullName : null,
            p.Employee != null ? p.Employee.Position : null,
            p.WalletId,
            p.Wallet != null ? p.Wallet.Name : null,
            p.Wallet != null ? p.Wallet.Type.ToString() : null,
            p.Components.Sum(c => c.Amount),
            string.Empty,
            p.Notes,
            p.Period,
            p.Salary,
            null,
            p.Components.Select(c => new PaymentSourceDto(
                c.Id,
                c.SourceType.ToString(),
                c.WalletId,
                c.Wallet != null ? c.Wallet.Name : null,
                c.Wallet != null ? c.Wallet.Type.ToString() : null,
                c.Amount)).ToArray(),
            p.Allocations.Select(a => new PaymentAllocationEntryDto(
                a.Id,
                a.Type.ToString(),
                a.TransactionId,
                a.Transaction != null ? a.Transaction.Type.ToString() : null,
                a.Amount)).ToArray(),
            p.Attachments.Select(a => new PaymentAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.Url)).ToArray(),
            // Echo the settled transaction's note (the single one this payment settles; first if several).
            p.Allocations
                .Where(a => a.Type == PaymentAllocationType.TransactionSettlement && a.Transaction != null)
                .Select(a => a.Transaction!.Notes)
                .FirstOrDefault(),
            // Echo the settled transaction(s)' attachments (union), stored on the transaction — not duplicated.
            p.Allocations
                .Where(a => a.Type == PaymentAllocationType.TransactionSettlement && a.Transaction != null)
                .SelectMany(a => a.Transaction!.Attachments)
                .Select(ta => new PaymentAttachmentDto(
                    ta.Id,
                    ta.FileName,
                    ta.ContentType,
                    ta.SizeBytes,
                    ta.Url)).ToArray());

    public Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A transaction's payments are its settling allocations, joined to the payment that made them.
        return context.PaymentAllocations
            .Where(a => a.TransactionId == request.TransactionId && a.Type == PaymentAllocationType.TransactionSettlement)
            .OrderByDescending(a => a.Payment.DateUtc)
            .Select(a => new TransactionPaymentDto(
                a.Id,
                a.TransactionId!.Value,
                a.PaymentId,
                a.Amount,
                a.Payment.Number.ToString(),
                a.Payment.WalletId,
                a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                a.Payment.Notes,
                a.Payment.DateUtc))
            .ToArrayAsync();
    }
}
