using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Activity;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using DomainPaymentType = Ombor.Domain.Enums.PaymentType;

namespace Ombor.Application.Services.Activity;

/// <summary>An operation's id and when it started.</summary>
internal sealed record ActivityOperation(Guid OperationId, DateTimeOffset At);

/// <summary>
/// The SQL side of the Activity Log: which audit rows match the filters, the page of operations they form, and the
/// rows of those operations. Every query goes through the organization filter on <see cref="AuditEntry"/>.
/// </summary>
internal sealed class ActivityQueries(IApplicationDbContext context, IBusinessClock clock)
{
    public IQueryable<AuditEntry> Matching(GetActivityRequest request)
    {
        var rows = context.AuditEntries.AsNoTracking().Where(a => a.OperationId != null);

        if (request.UserId is { } userId)
        {
            rows = rows.Where(a => a.UserId == userId);
        }

        if (request.From is { } from)
        {
            var start = clock.StartOfDay(from);
            rows = rows.Where(a => a.TimestampUtc >= start);
        }

        if (request.To is { } to)
        {
            var end = clock.StartOfDay(to.AddDays(1));
            rows = rows.Where(a => a.TimestampUtc < end);
        }

        if (request.EntityKind is { } kind)
        {
            rows = OfKind(rows, kind, request.EntityId);
        }

        if (request.Action is { } action)
        {
            rows = WithAction(rows, action, request.EntityKind);
        }

        return rows;
    }

    public async Task<(List<ActivityOperation> Page, int Total)> PageAsync(IQueryable<AuditEntry> rows, int page, int pageSize)
    {
        var operations = rows
            .GroupBy(a => a.OperationId!.Value)
            .Select(g => new { OperationId = g.Key, At = g.Min(a => a.TimestampUtc) });

        var total = await operations.CountAsync();
        var items = await operations
            .OrderByDescending(o => o.At)
            .ThenByDescending(o => o.OperationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return ([.. items.Select(o => new ActivityOperation(o.OperationId, o.At))], total);
    }

    /// <summary>
    /// The rows of the given operations, at most <paramref name="perOperation"/> each: the records themselves before
    /// their lines and stock rows, so a capped operation still shows what it was about.
    /// </summary>
    public Task<List<ActivityRowData>> RowsAsync(List<Guid> operationIds, int perOperation)
    {
        var parts = ActivityCatalog.PartTypes;

        // EF ranks the inner source with ROW_NUMBER() per operation; filtering it to the page's operations keeps that
        // window to the page's rows instead of the organization's whole log.
        var pageRows = context.AuditEntries
            .AsNoTracking()
            .Where(a => a.OperationId != null && operationIds.Contains(a.OperationId.Value));

        return pageRows
            .Select(a => a.OperationId!.Value)
            .Distinct()
            .SelectMany(operationId => pageRows
                .Where(b => b.OperationId == operationId)
                .OrderBy(b => parts.Contains(b.EntityType) ? 1 : 0)
                .ThenBy(b => b.Id)
                .Take(perOperation))
            .Select(b => new ActivityRowData(
                b.Id,
                b.OperationId!.Value,
                b.EntityType,
                b.EntityId,
                b.Action,
                b.ParentEntityType,
                b.ParentEntityId,
                b.OldValues,
                b.NewValues,
                b.UserId,
                b.TimestampUtc))
            .ToListAsync();
    }

    /// <summary>How many records each operation changed (a record saved twice in one request counts once).</summary>
    public Task<Dictionary<Guid, int>> CountsAsync(List<Guid> operationIds) =>
        context.AuditEntries
            .AsNoTracking()
            .Where(a => a.OperationId != null && operationIds.Contains(a.OperationId.Value))
            .GroupBy(a => a.OperationId!.Value)
            .Select(g => new { g.Key, Count = g.Select(a => a.EntityType + ":" + a.EntityId.ToString()).Distinct().Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    // With an id: that record and its lines (the «История» tab). Without: every record of the kind; a transaction
    // or payment kind narrows by the document's type.
    private IQueryable<AuditEntry> OfKind(IQueryable<AuditEntry> rows, ActivityEntityKind kind, int? entityId)
    {
        var entityType = ActivityCatalog.EntityTypeOf(kind);

        if (entityId is { } id)
        {
            return rows.Where(a =>
                (a.EntityType == entityType && a.EntityId == id)
                || (a.ParentEntityType == entityType && a.ParentEntityId == id));
        }

        rows = rows.Where(a => a.EntityType == entityType);

        if (ActivityCatalog.TransactionTypeOf(kind) is { } transactionType)
        {
            var ofType = context.Transactions.Where(t => t.Type == transactionType).Select(t => t.Id);

            return rows.Where(a => ofType.Contains(a.EntityId));
        }

        if (kind is ActivityEntityKind.Payment or ActivityEntityKind.Payroll)
        {
            var payroll = kind == ActivityEntityKind.Payroll;
            var ofType = context.Payments.Where(p => (p.Type == DomainPaymentType.Payroll) == payroll).Select(p => p.Id);

            return rows.Where(a => ofType.Contains(a.EntityId));
        }

        return rows;
    }

    // An action describes the records themselves: a sale's lines and stock rows, and (unless sales are asked for) a
    // document's settlement update, would otherwise make «Изменён» match every sale and payment.
    private static IQueryable<AuditEntry> WithAction(IQueryable<AuditEntry> rows, ActivityAction action, ActivityEntityKind? kind)
    {
        var stored = ActivityAssembler.ToAuditAction(action);
        var parts = ActivityCatalog.PartTypes;
        rows = rows.Where(a => a.Action == stored);

        if (kind is null || !ActivityCatalog.IsPart(ActivityCatalog.EntityTypeOf(kind.Value)))
        {
            rows = rows.Where(a => !parts.Contains(a.EntityType));
        }

        if (kind is null)
        {
            rows = rows.Where(a => !(a.EntityType == ActivityCatalog.Transaction && a.Action == AuditAction.Updated));
        }

        return rows;
    }
}
