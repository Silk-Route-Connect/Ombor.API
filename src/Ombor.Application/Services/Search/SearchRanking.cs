using Ombor.Application.Helpers;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Search;

namespace Ombor.Application.Services.Search;

/// <summary>
/// Matches candidates against a query and orders the hits: the whole value equal to the query first, then a value
/// starting with it, then a word inside it starting with it, then any other containment; active before archived;
/// then by label. A record counts once, on its best-matching field.
/// </summary>
internal static class SearchRanking
{
    private const int Exact = 0;
    private const int Prefix = 1;
    private const int WordPrefix = 2;
    private const int Contains = 3;

    public static SearchGroupDto Group(IEnumerable<SearchCandidate> candidates, SearchQuery query)
    {
        var hits = candidates
            .Select(candidate => (Candidate: candidate, Match: BestMatch(candidate, query)))
            .Where(hit => hit.Match is not null)
            .OrderBy(hit => hit.Match!.Value.Score)
            .ThenBy(hit => hit.Candidate.IsArchived)
            .ThenBy(hit => hit.Candidate.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.Candidate.Id)
            .ToArray();

        return new SearchGroupDto(
            hits.Length,
            [.. hits.Take(query.Limit).Select(hit => new SearchHitDto(
                hit.Candidate.Kind,
                hit.Candidate.Id,
                hit.Candidate.Label,
                hit.Candidate.Detail,
                hit.Match!.Value.Field,
                hit.Candidate.IsArchived))]);
    }

    private static (int Score, SearchMatch Field)? BestMatch(SearchCandidate candidate, SearchQuery query)
    {
        (int Score, SearchMatch Field)? best = null;

        foreach (var field in candidate.Fields)
        {
            var score = field.Match == SearchMatch.Phone ? PhoneScore(field.Value, query) : TextScore(field.Value, query);

            if (score is { } s && (best is null || s < best.Value.Score))
            {
                best = (s, field.Match);
            }
        }

        return best;
    }

    private static int? TextScore(string? value, SearchQuery query)
    {
        if (query.Skeleton.Length == 0)
        {
            return null;
        }

        var text = SearchText.Skeleton(value);

        if (text.Length == 0)
        {
            return null;
        }

        if (text == query.Skeleton)
        {
            return Exact;
        }

        if (text.StartsWith(query.Skeleton, StringComparison.Ordinal))
        {
            return Prefix;
        }

        if (text.Contains(" " + query.Skeleton, StringComparison.Ordinal))
        {
            return WordPrefix;
        }

        return text.Contains(query.Skeleton, StringComparison.Ordinal) ? Contains : null;
    }

    // Stored phones may or may not carry the country code, so a full national number typed either way is exact.
    private static int? PhoneScore(string? value, SearchQuery query)
    {
        if (query.PhoneDigits is not { } wanted)
        {
            return null;
        }

        var digits = SearchText.Digits(value);

        if (digits.Length == 0 || !digits.Contains(wanted, StringComparison.Ordinal))
        {
            return null;
        }

        return digits == wanted || (wanted.Length >= 9 && digits.EndsWith(wanted, StringComparison.Ordinal)) ? Exact : Contains;
    }
}
