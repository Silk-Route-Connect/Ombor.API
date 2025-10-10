using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request to retrieve a list of partners with optional filtering
/// </summary>
/// <param name="SearchTerm">
///  Optional case-insensitive term to filter by Name, Address, Email, CompanyName
/// </param>
public sealed class GetPartnersRequest : PagedRequest
{
    public PartnerType? Type { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "name_asc";

    public GetPartnersRequest() { }

    public GetPartnersRequest(
        PartnerType? type = null,
        string? searchTerm = null,
        string? sortBy = "name_asc",
        int pageNumber = 1,
        int pageSize = 10) : base(pageNumber, pageSize)
    {
        Type = type;
        SearchTerm = searchTerm;
        SortBy = sortBy;
    }
}
