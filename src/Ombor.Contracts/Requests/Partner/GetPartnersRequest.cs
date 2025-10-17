using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request to retrieve a list of partners with optional filtering
/// </summary>
/// <param name="SearchTerm">
///  Optional case-insensitive term to filter by Name, Address, Email, CompanyName
/// </param>
public sealed record GetPartnersRequest(
    PartnerType? Type = null,
    string? SearchTerm = null,
    string? SortBy = "name_asc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);
