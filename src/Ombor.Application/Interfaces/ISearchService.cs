using FluentValidation;
using Ombor.Contracts.Requests.Search;
using Ombor.Contracts.Responses.Search;

namespace Ombor.Application.Interfaces;

/// <summary>Global search over the organization's partners, products, documents, employees, warehouses and wallets.</summary>
public interface ISearchService
{
    /// <summary>Finds the records matching the query, grouped by kind, best matches first.</summary>
    /// <exception cref="ValidationException">If the query is blank or too long, or the limit is out of range.</exception>
    Task<SearchResultsDto> SearchAsync(SearchRequest request);
}
