using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Ombor.Contracts.Responses.Activity;
using Ombor.Domain.Common;

namespace Ombor.Application.Services.Activity;

/// <summary>
/// Turns one audit row's stored columns into the fields a person reads: camelCase names, enum names instead of the
/// numbers older rows stored, references resolved to names or document numbers, bookkeeping columns dropped and a
/// masked secret named without values.
/// </summary>
internal static class ActivityFields
{
    private const string EntityNamespace = "Ombor.Domain.Entities.";

    private static readonly ConcurrentDictionary<string, Type?> EntityTypes = new();

    public static ActivityFieldChangeDto[] Build(ActivityRow row, ActivityLookups lookups)
    {
        var entityType = EntityTypeOf(row.EntityType) ?? typeof(object);
        var parentKey = ActivityCatalog.ParentKeyOf(row.EntityType);
        var fields = new List<ActivityFieldChangeDto>();

        foreach (var column in row.Old.Keys.Union(row.New.Keys))
        {
            if (column == parentKey || (!column.Contains('.') && AuditFieldPolicy.IsExcluded(entityType, column)))
            {
                continue;
            }

            if (AuditFieldPolicy.IsMaskedField(entityType, column))
            {
                fields.Add(new ActivityFieldChangeDto(CamelCase(column), null, null));
                continue;
            }

            var old = Normalize(entityType, column, row.Old);
            var @new = Normalize(entityType, column, row.New);

            // Rows recorded before 2026-10-04 by the seed list unchanged columns as updated.
            if ((old is null && @new is null) || (old is { } before && @new is { } after && before.GetRawText() == after.GetRawText()))
            {
                continue;
            }

            if (ActivityCatalog.References.TryGetValue(column, out var target))
            {
                fields.Add(new ActivityFieldChangeDto(
                    CamelCase(column[..^2]),
                    Resolve(target, old, lookups),
                    Resolve(target, @new, lookups)));
                continue;
            }

            fields.Add(new ActivityFieldChangeDto(CamelCase(column), old, @new));
        }

        return [.. fields];
    }

    /// <summary>The domain class an audit row's entity type names, for its property types and audit attributes.</summary>
    public static Type? EntityTypeOf(string entityType) =>
        EntityTypes.GetOrAdd(entityType, name => typeof(EntityBase).Assembly.GetType(EntityNamespace + name));

    /// <summary>A reference shown as the record's name or document number; the raw id when it no longer exists.</summary>
    private static JsonElement? Resolve(string target, JsonElement? value, ActivityLookups lookups)
    {
        if (value is not { ValueKind: JsonValueKind.Number } number || !number.TryGetInt32(out var id))
        {
            return value;
        }

        var label = target == ActivityCatalog.Transaction
            ? lookups.Document(target, id)?.Number?.ToString()
            : lookups.Name(target, id);

        return label is null ? value : JsonSerializer.SerializeToElement(label);
    }

    // Rows recorded before enums were stored by name hold their numbers.
    private static JsonElement? Normalize(Type entityType, string column, IReadOnlyDictionary<string, JsonElement> values)
    {
        if (!values.TryGetValue(column, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number
            && PropertyTypeOf(entityType, column) is { IsEnum: true } enumType
            && value.TryGetInt64(out var number)
            && Enum.GetName(enumType, Enum.ToObject(enumType, number)) is { } name)
        {
            return JsonSerializer.SerializeToElement(name);
        }

        return value;
    }

    private static Type? PropertyTypeOf(Type entityType, string column)
    {
        var type = entityType;

        foreach (var segment in column.Split('.'))
        {
            var property = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);

            if (property is null)
            {
                return null;
            }

            type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }

        return type;
    }

    private static string CamelCase(string column) =>
        string.Join('.', column.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
