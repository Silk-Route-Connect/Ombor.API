using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Activity;

/// <summary>Filters and page of the Activity Log. Every filter is optional; filters combine with AND.</summary>
/// <param name="EntityKind">Only operations that touched a record of this kind.</param>
/// <param name="EntityId">With <paramref name="EntityKind"/>: only operations that touched this record or one of its
/// lines — the «История» tab of a detail page.</param>
/// <param name="UserId">Only operations made by this user (deactivated users included).</param>
/// <param name="Action">Only operations in which a record was created / updated / deleted / archived / restored. A
/// document's lines and stock rows do not count, so «Изменён» does not match every sale.</param>
/// <param name="From">First local (Tashkent) calendar day, inclusive.</param>
/// <param name="To">Last local (Tashkent) calendar day, inclusive.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Operations per page, 1–100.</param>
public sealed record GetActivityRequest(
    ActivityEntityKind? EntityKind = null,
    int? EntityId = null,
    int? UserId = null,
    ActivityAction? Action = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20);
