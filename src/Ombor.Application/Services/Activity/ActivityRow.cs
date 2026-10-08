using System.Text.Json;
using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Activity;

/// <summary>One stored audit row, as the Activity Log reads it.</summary>
internal sealed record ActivityRowData(
    int Id,
    Guid OperationId,
    string EntityType,
    int EntityId,
    AuditAction Action,
    string? ParentEntityType,
    int? ParentEntityId,
    string? OldValues,
    string? NewValues,
    int? UserId,
    DateTimeOffset TimestampUtc);

/// <summary>An audit row with its JSON values parsed and its ranking computed.</summary>
internal sealed class ActivityRow
{
    public ActivityRow(ActivityRowData data)
        : this(data, Parse(data.OldValues), Parse(data.NewValues))
    {
    }

    private ActivityRow(ActivityRowData data, IReadOnlyDictionary<string, JsonElement> old, IReadOnlyDictionary<string, JsonElement> @new)
    {
        Data = data;
        Old = old;
        New = @new;
        Rank = ActivityCatalog.RankOf(data.EntityType, data.Action);
    }

    /// <summary>
    /// One record's rows within one operation (oldest first) as its net change: a sale saved, then settled in the same
    /// request, reads as one creation with its final values; two edits read as one, from the first old value to the
    /// last new one.
    /// </summary>
    public static ActivityRow Merge(IReadOnlyList<ActivityRow> rows)
    {
        if (rows.Count == 1)
        {
            return rows[0];
        }

        var first = rows[0];
        var last = rows[^1];
        var latest = rows.SelectMany(row => row.New).GroupBy(pair => pair.Key).ToDictionary(g => g.Key, g => g.Last().Value);

        if (first.Action == AuditAction.Created)
        {
            return last.Action == AuditAction.Deleted
                ? new ActivityRow(first.Data with { Action = AuditAction.Deleted }, latest, new Dictionary<string, JsonElement>())
                : new ActivityRow(first.Data, new Dictionary<string, JsonElement>(), latest);
        }

        var earliest = rows.SelectMany(row => row.Old).GroupBy(pair => pair.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var action = last.Action == AuditAction.Deleted
            ? AuditAction.Deleted
            : rows.LastOrDefault(row => row.Action is AuditAction.Archived or AuditAction.Restored)?.Action ?? AuditAction.Updated;

        if (action == AuditAction.Deleted)
        {
            return new ActivityRow(first.Data with { Action = action }, earliest, new Dictionary<string, JsonElement>());
        }

        var entityType = ActivityFields.EntityTypeOf(first.EntityType) ?? typeof(object);
        var changed = earliest.Keys.Union(latest.Keys)
            .Where(key => AuditFieldPolicy.IsMaskedField(entityType, key) || RawText(earliest, key) != RawText(latest, key))
            .ToHashSet();

        return new ActivityRow(
            first.Data with { Action = action },
            earliest.Where(pair => changed.Contains(pair.Key)).ToDictionary(),
            latest.Where(pair => changed.Contains(pair.Key)).ToDictionary());
    }

    public ActivityRowData Data { get; }

    public IReadOnlyDictionary<string, JsonElement> Old { get; }

    public IReadOnlyDictionary<string, JsonElement> New { get; }

    public int Rank { get; }

    public string EntityType => Data.EntityType;

    public int EntityId => Data.EntityId;

    public AuditAction Action => Data.Action;

    /// <summary>A column's latest recorded value: after the change, else before it.</summary>
    public JsonElement? Value(string column) =>
        New.TryGetValue(column, out var value) || Old.TryGetValue(column, out value) ? value : null;

    public int? IntValue(string column) =>
        Value(column) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var number) ? number : null;

    public decimal? DecimalValue(string column) =>
        Value(column) is { ValueKind: JsonValueKind.Number } value && value.TryGetDecimal(out var number) ? number : null;

    public string? StringValue(string column) =>
        Value(column) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static string RawText(IReadOnlyDictionary<string, JsonElement> values, string key) =>
        values.TryGetValue(key, out var value) ? value.GetRawText() : "null";

    private static Dictionary<string, JsonElement> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);

        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
            : [];
    }
}
