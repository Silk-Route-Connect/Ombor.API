using Ombor.Application.Services.Search;
using Ombor.Contracts.Enums;

namespace Ombor.Tests.Unit.Services;

public sealed class SearchRankingTests
{
    [Fact]
    public void Group_OrdersExactPrefixWordContains_ActiveBeforeArchived_AndCountsEveryMatch()
    {
        SearchCandidate[] candidates =
        [
            Partner(1, "Super Ali"),
            Partner(2, "Alimov"),
            Partner(3, "Ali", archived: true),
            Partner(4, "Vali"),
            Partner(5, "Ali"),
            Partner(6, "Bobur"),
        ];

        var group = SearchRanking.Group(candidates, SearchQuery.Of("ali", limit: 3));

        Assert.Equal(5, group.Total);
        Assert.Equal([5, 3, 2], group.Items.Select(h => h.Id));
        Assert.True(group.Items[1].IsArchived);
    }

    [Fact]
    public void Group_MatchesARecordOnce_OnItsBestField()
    {
        var candidate = new SearchCandidate(
            ActivityEntityKind.Partner, 1, "Market 901234567", null, false,
            [new(SearchMatch.Name, "Market 901234567"), new(SearchMatch.Phone, "+998 90 123 45 67")]);

        var group = SearchRanking.Group([candidate], SearchQuery.Of("90 123 45 67", limit: 5));

        var hit = Assert.Single(group.Items);
        Assert.Equal(SearchMatch.Phone, hit.MatchedOn);
    }

    [Fact]
    public void Group_IgnoresPhonesForAShortOrTextQuery()
    {
        var candidate = new SearchCandidate(
            ActivityEntityKind.Partner, 1, "Bobur", null, false,
            [new(SearchMatch.Name, "Bobur"), new(SearchMatch.Phone, "+998901234567")]);

        Assert.Equal(0, SearchRanking.Group([candidate], SearchQuery.Of("998", limit: 5)).Total);
        Assert.Equal(0, SearchRanking.Group([candidate], SearchQuery.Of("Ali 1234", limit: 5)).Total);
    }

    private static SearchCandidate Partner(int id, string name, bool archived = false) =>
        new(ActivityEntityKind.Partner, id, name, null, archived, [new(SearchMatch.Name, name)]);
}
