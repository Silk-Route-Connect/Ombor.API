using Ombor.Application.Helpers;
using Ombor.Contracts.Enums;

namespace Ombor.Application.Services.Search;

/// <summary>A query reduced once to every form the matchers compare.</summary>
/// <param name="Text">The trimmed query.</param>
/// <param name="Skeleton">Its <see cref="SearchText.Skeleton"/> — compared with names, SKUs and barcodes.</param>
/// <param name="PhoneDigits">Its digits when it looks like a phone number; null otherwise.</param>
/// <param name="DocumentNumber">The document number it names; null when it is not a number.</param>
/// <param name="Limit">The most records returned per group.</param>
internal sealed record SearchQuery(string Text, string Skeleton, string? PhoneDigits, int? DocumentNumber, int Limit)
{
    /// <summary>Fewer digits than this match too many numbers to be useful («998» is in every phone).</summary>
    public const int MinimumPhoneDigits = 4;

    public static SearchQuery Of(string query, int limit)
    {
        var text = query.Trim();

        return new SearchQuery(
            text,
            SearchText.Skeleton(text),
            SearchText.IsPhoneQuery(text, MinimumPhoneDigits) ? SearchText.Digits(text) : null,
            SearchText.DocumentNumber(text),
            limit);
    }
}

/// <summary>A record that may match, with the fields to compare in priority order (the name first).</summary>
internal sealed record SearchCandidate(
    ActivityEntityKind Kind,
    int Id,
    string Label,
    string? Detail,
    bool IsArchived,
    IReadOnlyList<SearchField> Fields);

/// <summary>A searchable value of a record; <see cref="SearchMatch.Phone"/> values are compared by digits.</summary>
internal readonly record struct SearchField(SearchMatch Match, string? Value);
