using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ombor.Application.Interfaces;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Records every change to an <see cref="IAuditable"/> entity (money/stock events and master data) as an
/// immutable <see cref="AuditEntry"/> row, written in the same transaction as the change. Rows written while
/// serving one HTTP request share one operation id; outside a request every save is its own operation.
/// </summary>
internal sealed class AuditSaveChangesInterceptor(
    ICurrentUserAccessor currentUser,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly List<AuditEntry> _captured = [];

    // Audit rows for inserts: the source row's identity key (and a new parent's) is only known after the save.
    private readonly List<(AuditEntry Audit, object Source)> _pendingInserts = [];

    private Guid? _requestOperationId;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Capture(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Capture(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _captured.Clear();

        if (eventData.Context is not null && ApplyBackfill())
        {
            eventData.Context.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _captured.Clear();

        if (eventData.Context is not null && ApplyBackfill())
        {
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);

        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext context)
    {
        // Start clean: a previous failed save may have left stale entries.
        _pendingInserts.Clear();
        _captured.Clear();

        var changes = AuditChangeReader.Read(context.ChangeTracker);

        if (changes.Count == 0)
        {
            return;
        }

        var operationId = NextOperationId();
        var timestamp = DateTimeOffset.UtcNow;

        foreach (var change in changes)
        {
            var entity = change.Entry.Entity;
            var parent = (entity as IAuditableChild)?.AuditParent;

            var audit = new AuditEntry
            {
                OperationId = operationId,
                EntityType = change.Entry.Metadata.ClrType.Name,
                EntityId = ((EntityBase)entity).Id,
                Action = change.Action,
                ParentEntityType = parent?.EntityType.Name,
                ParentEntityId = parent?.EntityId,
                OldValues = change.OldValues,
                NewValues = change.NewValues,
                UserId = currentUser.UserId,
                TimestampUtc = timestamp,
                OrganizationId = OrganizationIdOf(entity),
            };

            context.Add(audit);
            _captured.Add(audit);

            if (change.Entry.State == EntityState.Added)
            {
                _pendingInserts.Add((audit, entity));
            }
        }
    }

    private bool ApplyBackfill()
    {
        if (_pendingInserts.Count == 0)
        {
            return false;
        }

        foreach (var (audit, source) in _pendingInserts)
        {
            audit.EntityId = ((EntityBase)source).Id;
            audit.OrganizationId = OrganizationIdOf(source);

            if (source is IAuditableChild child)
            {
                audit.ParentEntityId = child.AuditParent.EntityId;
            }
        }

        _pendingInserts.Clear();

        return true;
    }

    // A failed save leaves its audit rows tracked as Added; a retried save must not persist them a second time.
    private void Discard(DbContext? context)
    {
        if (context is not null)
        {
            foreach (var audit in _captured)
            {
                context.Entry(audit).State = EntityState.Detached;
            }
        }

        _captured.Clear();
        _pendingInserts.Clear();
    }

    private Guid NextOperationId() =>
        httpContextAccessor.HttpContext is null ? Guid.NewGuid() : _requestOperationId ??= Guid.NewGuid();

    // An organization is audited under its own id: it is the tenant, not scoped to one.
    private static int OrganizationIdOf(object entity) => entity switch
    {
        IOrganizationScoped scoped => scoped.OrganizationId,
        Organization organization => organization.Id,
        _ => 0,
    };
}
