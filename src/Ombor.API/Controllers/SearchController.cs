using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Search;
using Ombor.Contracts.Responses.Search;

namespace Ombor.API.Controllers;

/// <summary>
/// Global search (the topbar search box): partners, products, documents by number, employees, warehouses and wallets
/// of the current organization in one call. Read-only.
/// </summary>
[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService service) : ControllerBase
{
    /// <summary>
    /// Finds records by name, company, phone, SKU, barcode or document number — case-insensitive, Cyrillic and Latin
    /// finding each other, archived records included and flagged.
    /// </summary>
    /// <param name="request">The query (<c>q</c>) and the most records per group (<c>limit</c>, default 5).</param>
    /// <returns>One group per record kind, best matches first, with the total number of matches.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResultsDto>> GetAsync([FromQuery] SearchRequest request)
    {
        var response = await service.SearchAsync(request);

        return Ok(response);
    }
}
