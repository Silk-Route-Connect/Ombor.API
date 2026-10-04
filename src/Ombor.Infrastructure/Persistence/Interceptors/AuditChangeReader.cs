using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Infrastructure.Persistence.Interceptors;

/// <summary>One audited entity's pending change, ready to become an audit row.</summary>
internal sealed record AuditChange(EntityEntry Entry, AuditAction Action, string? OldValues, string? NewValues);

/// <summary>
/// Reads the change tracker's pending changes as audit changes: one per changed <see cref="IAuditable"/> entity.
/// Complex properties and owned types are folded into their owner as dotted columns ("Packaging.Size",
/// "ContactInfo.Email"), so a product's packaging or an employee's contacts are part of that record's history.
/// </summary>
internal static class AuditChangeReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IReadOnlyList<AuditChange> Read(ChangeTracker tracker)
    {
        var entries = tracker.Entries().ToList();
        var owners = new List<EntityEntry>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var entry in entries.Where(e => IsPending(e.State)))
        {
            var owner = entry.Metadata.IsOwned() ? FindOwner(entry, entries) : entry;

            if (owner?.Entity is IAuditable && seen.Add(owner.Entity))
            {
                owners.Add(owner);
            }
        }

        return owners.Select(owner => Describe(owner, entries)).OfType<AuditChange>().ToList();
    }

    private static AuditChange? Describe(EntityEntry owner, IReadOnlyList<EntityEntry> entries)
    {
        var snapshot = new Snapshot();
        var hasBefore = owner.State != EntityState.Added;
        var hasAfter = owner.State != EntityState.Deleted;

        snapshot.AddProperties(owner.Properties, owner.Metadata.ClrType, prefix: string.Empty, hasBefore, hasAfter);
        AddComplexProperties(snapshot, owner.ComplexProperties, prefix: string.Empty, hasBefore, hasAfter);
        AddOwnedReferences(snapshot, owner, entries, hasBefore, hasAfter);

        return owner.State switch
        {
            EntityState.Added => new AuditChange(owner, AuditAction.Created, null, Serialize(WithoutNulls(snapshot.After))),
            EntityState.Deleted => new AuditChange(owner, AuditAction.Deleted, Serialize(WithoutNulls(snapshot.Before)), null),
            _ => DescribeUpdate(owner, snapshot),
        };
    }

    private static AuditChange? DescribeUpdate(EntityEntry owner, Snapshot snapshot)
    {
        var changed = snapshot.Before.Keys
            .Union(snapshot.After.Keys)
            .Where(key => !snapshot.Comparers[key].Equals(snapshot.Before.GetValueOrDefault(key), snapshot.After.GetValueOrDefault(key)))
            .ToList();

        var before = changed.ToDictionary(key => key, key => snapshot.Before.GetValueOrDefault(key));
        var after = changed.ToDictionary(key => key, key => snapshot.After.GetValueOrDefault(key));

        foreach (var masked in snapshot.MaskedChanges)
        {
            before[masked] = null;
            after[masked] = null;
        }

        if (before.Count == 0)
        {
            return null;
        }

        var action = after.TryGetValue(AuditFieldPolicy.ArchiveFlag, out var archived) && archived is bool flag
            ? flag ? AuditAction.Archived : AuditAction.Restored
            : AuditAction.Updated;

        return new AuditChange(owner, action, Serialize(before), Serialize(after));
    }

    private static void AddComplexProperties(
        Snapshot snapshot, IEnumerable<ComplexPropertyEntry> complexProperties, string prefix, bool hasBefore, bool hasAfter)
    {
        foreach (var complex in complexProperties)
        {
            var path = $"{prefix}{complex.Metadata.Name}.";

            snapshot.AddProperties(complex.Properties, complex.Metadata.ComplexType.ClrType, path, hasBefore, hasAfter);
            AddComplexProperties(snapshot, complex.ComplexProperties, path, hasBefore, hasAfter);
        }
    }

    // An owned reference that is replaced shows up as a Deleted entry (old values) plus an Added one (new values);
    // one mutated in place is a single Modified entry carrying both.
    private static void AddOwnedReferences(
        Snapshot snapshot, EntityEntry owner, IReadOnlyList<EntityEntry> entries, bool hasBefore, bool hasAfter)
    {
        foreach (var navigation in owner.Metadata.GetNavigations().Where(n => n.TargetEntityType.IsOwned() && !n.IsCollection))
        {
            var owned = entries
                .Where(e => e.Metadata == navigation.TargetEntityType && FindOwner(e, [owner]) is not null)
                .ToList();
            var path = $"{navigation.Name}.";
            var type = navigation.TargetEntityType.ClrType;

            if (hasBefore && owned.FirstOrDefault(e => e.State is not EntityState.Added) is { } previous)
            {
                snapshot.AddProperties(previous.Properties, type, path, hasBefore: true, hasAfter: false);
            }

            if (hasAfter && owned.FirstOrDefault(e => e.State is not EntityState.Deleted) is { } current)
            {
                snapshot.AddProperties(current.Properties, type, path, hasBefore: false, hasAfter: true);
            }
        }
    }

    private static EntityEntry? FindOwner(EntityEntry ownedEntry, IReadOnlyList<EntityEntry> candidates)
    {
        var ownership = ownedEntry.Metadata.FindOwnership();

        if (ownership is null)
        {
            return null;
        }

        var foreignKey = ownership.Properties.Select(p => ownedEntry.Property(p.Name).CurrentValue).ToArray();

        return candidates.FirstOrDefault(candidate =>
            candidate.Metadata == ownership.PrincipalEntityType
            && ownership.PrincipalKey.Properties
                .Select(p => candidate.Property(p.Name).CurrentValue)
                .SequenceEqual(foreignKey));
    }

    private static bool IsPending(EntityState state) =>
        state is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private static Dictionary<string, object?> WithoutNulls(Dictionary<string, object?> values) =>
        values.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static string? Serialize(Dictionary<string, object?> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);

    /// <summary>
    /// The audited columns of one record before and after the change, each with EF's comparer for its type — so
    /// 100.00 read back from SQL equals the 100 a request sent, and a list of phone numbers compares by content.
    /// </summary>
    private sealed class Snapshot
    {
        public Dictionary<string, object?> Before { get; } = [];
        public Dictionary<string, object?> After { get; } = [];
        public Dictionary<string, ValueComparer> Comparers { get; } = [];
        public HashSet<string> MaskedChanges { get; } = [];

        public void AddProperties(IEnumerable<PropertyEntry> properties, Type declaringType, string prefix, bool hasBefore, bool hasAfter)
        {
            foreach (var property in properties.Where(p => IsRecorded(p.Metadata, declaringType)))
            {
                var name = property.Metadata.Name;
                var comparer = property.Metadata.GetValueComparer();

                if (AuditFieldPolicy.MaskedName(declaringType, name) is { } maskedAs)
                {
                    if (hasBefore && hasAfter && !comparer.Equals(property.OriginalValue, property.CurrentValue))
                    {
                        MaskedChanges.Add(prefix + maskedAs);
                    }

                    continue;
                }

                Comparers[prefix + name] = comparer;

                if (hasBefore)
                {
                    Before[prefix + name] = property.OriginalValue;
                }

                if (hasAfter)
                {
                    After[prefix + name] = property.CurrentValue;
                }
            }
        }

        private static bool IsRecorded(IProperty property, Type declaringType) =>
            !property.IsPrimaryKey()
            && !property.IsShadowProperty()
            && !property.IsConcurrencyToken
            && !AuditFieldPolicy.IsExcluded(declaringType, property.Name);
    }
}
