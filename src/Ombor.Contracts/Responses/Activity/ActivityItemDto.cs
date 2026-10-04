using System.Text.Json;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Activity;

/// <summary>A page of the Activity Log, newest operation first.</summary>
/// <param name="Items">The operations on this page.</param>
/// <param name="Total">How many operations match the filters across all pages.</param>
public sealed record ActivityPageDto(ActivityItemDto[] Items, int Total);

/// <summary>
/// One operation: everything one request changed (a sale with its lines, stock and payment; a product edit).
/// </summary>
/// <param name="OperationId">The operation id (opens the full operation via <c>GET /api/activity/{operationId}</c>).</param>
/// <param name="At">When the operation happened (UTC).</param>
/// <param name="Actor">Who did it; null for the system (seeding, registration before sign-in) — shown as «Система».</param>
/// <param name="Kind">What the operation did, as one value (e.g. <c>SaleCreated</c>, <c>ProductArchived</c>).</param>
/// <param name="Primary">The record the operation is about.</param>
/// <param name="Amount">The money figure of a money operation (document total, payment amount, written-off value,
/// opening balance); null otherwise.</param>
/// <param name="Changes">Every record the operation changed, the primary record first. The list is capped at 50 in
/// the list endpoint — <paramref name="ChangeCount"/> says how many there are.</param>
/// <param name="ChangeCount">How many records the operation changed in total.</param>
public sealed record ActivityItemDto(
    Guid OperationId,
    DateTimeOffset At,
    ActivityActorDto? Actor,
    ActivityKind Kind,
    ActivityRecordDto Primary,
    decimal? Amount,
    ActivityChangeDto[] Changes,
    int ChangeCount);

/// <summary>The user behind an operation.</summary>
/// <param name="Id">The user id.</param>
/// <param name="Name">First and last name.</param>
public sealed record ActivityActorDto(int Id, string Name);

/// <summary>A record an operation touched.</summary>
/// <param name="EntityKind">The kind of record.</param>
/// <param name="EntityId">Its id.</param>
/// <param name="Label">Its document number (bare — the client prepends «№») or name; null when it has none.</param>
public sealed record ActivityRecordDto(ActivityEntityKind EntityKind, int EntityId, string? Label);

/// <summary>What happened to one record in an operation.</summary>
/// <param name="EntityKind">The kind of record.</param>
/// <param name="EntityId">Its id.</param>
/// <param name="Label">Its document number or name (as in <see cref="ActivityRecordDto.Label"/>).</param>
/// <param name="Action">What happened to it.</param>
/// <param name="Fields">The values: every recorded field of a created or deleted record, only the changed ones of an
/// update.</param>
public sealed record ActivityChangeDto(
    ActivityEntityKind EntityKind,
    int EntityId,
    string? Label,
    ActivityAction Action,
    ActivityFieldChangeDto[] Fields);

/// <summary>
/// One field's value before and after. A reference to a partner, product, warehouse, wallet, category, employee or
/// document is served as that record's name or number under the field name without «Id» (<c>partnerId</c> →
/// <c>partner</c>); the raw id is served when the record no longer exists. A secret (the password) is served by name
/// only, never with values.
/// </summary>
/// <param name="Field">camelCase field name; a part of a record is dotted (<c>packaging.size</c>).</param>
/// <param name="Old">The value before; absent for a created record or a value that was empty.</param>
/// <param name="New">The value after; absent for a deleted record or a value that became empty.</param>
public sealed record ActivityFieldChangeDto(string Field, JsonElement? Old, JsonElement? New);
