using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Search;
using Ombor.Contracts.Responses.Search;

namespace Ombor.Application.Services.Search;

internal sealed class SearchService(
    IRequestValidator validator,
    SearchCandidates candidates,
    SearchDocuments documents) : ISearchService
{
    public async Task<SearchResultsDto> SearchAsync(SearchRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = SearchQuery.Of(request.Q!, request.Limit);

        // One DbContext per request, so the loads run one after another.
        var partners = SearchRanking.Group(await candidates.PartnersAsync(), query);
        var products = SearchRanking.Group(await candidates.ProductsAsync(), query);
        var employees = SearchRanking.Group(await candidates.EmployeesAsync(), query);
        var warehouses = SearchRanking.Group(await candidates.WarehousesAsync(), query);
        var wallets = SearchRanking.Group(await candidates.WalletsAsync(), query);
        var found = query.DocumentNumber is { } number
            ? await documents.ByNumberAsync(number, query.Limit)
            : new SearchGroupDto(0, []);

        return new SearchResultsDto(query.Text, partners, products, found, employees, warehouses, wallets);
    }
}
