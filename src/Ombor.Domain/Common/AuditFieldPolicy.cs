using System.Collections.Concurrent;
using System.Reflection;

namespace Ombor.Domain.Common;

/// <summary>
/// Which columns of an audited entity reach the audit log. One policy for the writer (the interceptor) and the
/// reader (the Activity Log), so rows recorded before a column was excluded are read the same way.
/// </summary>
public static class AuditFieldPolicy
{
    /// <summary>The flag whose flip records an archive or restore instead of a plain update.</summary>
    public const string ArchiveFlag = "IsArchived";

    // Bookkeeping the audit row already carries (actor, timestamp, organization) or that no write path sets.
    private static readonly HashSet<string> Noise = new(StringComparer.Ordinal)
    {
        nameof(EntityBase.Id),
        nameof(IOrganizationScoped.OrganizationId),
        nameof(AuditableEntity.CreatedAt),
        nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.UpdatedAt),
        nameof(AuditableEntity.UpdatedBy),
        nameof(AuditableEntity.IsDeleted),
        "CreatedById",
    };

    private static readonly ConcurrentDictionary<(Type, string), NotAuditedAttribute?> Attributes = new();
    private static readonly ConcurrentDictionary<Type, HashSet<string>> MaskedNames = new();

    /// <summary>Whether the column is left out entirely (noise, or a secret with no masked name).</summary>
    public static bool IsExcluded(Type entityType, string propertyName) =>
        Noise.Contains(propertyName) || (FindAttribute(entityType, propertyName) is { MaskedAs: null });

    /// <summary>The name a change to this column is recorded under without values, or null when it is logged normally.</summary>
    public static string? MaskedName(Type entityType, string propertyName) =>
        FindAttribute(entityType, propertyName)?.MaskedAs;

    /// <summary>Whether a recorded field is a masked secret (logged by name only), e.g. "Password" on a user.</summary>
    public static bool IsMaskedField(Type entityType, string fieldName) =>
        MaskedNames.GetOrAdd(
                entityType,
                type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.GetCustomAttribute<NotAuditedAttribute>()?.MaskedAs)
                    .OfType<string>()
                    .ToHashSet(StringComparer.Ordinal))
            .Contains(fieldName);

    private static NotAuditedAttribute? FindAttribute(Type entityType, string propertyName) =>
        Attributes.GetOrAdd(
            (entityType, propertyName),
            key => key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance)
                ?.GetCustomAttribute<NotAuditedAttribute>());
}
