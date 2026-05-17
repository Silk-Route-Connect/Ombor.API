using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ombor.Application.Interfaces;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Records every change to an <see cref="IAuditable"/> entity (money/stock events) as an
/// immutable <see cref="AuditEntry"/> row, written in the same transaction as the change.
/// </summary>
internal sealed class AuditSaveChangesInterceptor(ICurrentUserAccessor currentUser) : SaveChangesInterceptor
{
    // Audit rows for inserts: the source row's identity key is only known after the save.
    private readonly List<(AuditEntry Audit, EntityEntry Source)> _pendingInserts = [];

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
        if (eventData.Context is not null && ApplyBackfill())
        {
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext context)
    {
        // Start clean: a previous failed save may have left stale entries.
        _pendingInserts.Clear();

        var audited = context.ChangeTracker
            .Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in audited)
        {
            var (oldValues, newValues) = SerializeValues(entry);

            var audit = new AuditEntry
            {
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entry.State == EntityState.Added ? 0 : GetId(entry),
                Action = ToAction(entry.State),
                OldValues = oldValues,
                NewValues = newValues,
                UserId = currentUser.UserId,
                TimestampUtc = DateTimeOffset.UtcNow,
                TenantId = (entry.Entity as ITenantScoped)?.TenantId ?? 0,
            };

            context.Add(audit);

            if (entry.State == EntityState.Added)
            {
                _pendingInserts.Add((audit, entry));
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
            audit.EntityId = GetId(source);
        }

        _pendingInserts.Clear();

        return true;
    }

    private static int GetId(EntityEntry entry)
        => (int)(entry.Property(nameof(EntityBase.Id)).CurrentValue ?? 0);

    private static AuditAction ToAction(EntityState state) => state switch
    {
        EntityState.Added => AuditAction.Created,
        EntityState.Deleted => AuditAction.Deleted,
        _ => AuditAction.Updated,
    };

    private static (string? Old, string? New) SerializeValues(EntityEntry entry)
    {
        var properties = entry.Properties.Where(p => !p.Metadata.IsPrimaryKey());

        return entry.State switch
        {
            EntityState.Added => (null, Serialize(properties, p => p.CurrentValue)),
            EntityState.Deleted => (Serialize(properties, p => p.OriginalValue), null),
            _ => SerializeModified(properties),
        };
    }

    private static (string? Old, string? New) SerializeModified(IEnumerable<PropertyEntry> properties)
    {
        var changed = properties.Where(p => p.IsModified).ToList();

        return (Serialize(changed, p => p.OriginalValue), Serialize(changed, p => p.CurrentValue));
    }

    private static string? Serialize(IEnumerable<PropertyEntry> properties, Func<PropertyEntry, object?> selector)
    {
        var values = properties.ToDictionary(p => p.Metadata.Name, selector);

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }
}
