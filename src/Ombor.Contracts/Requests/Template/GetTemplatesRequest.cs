using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Template;

public sealed record GetTemplatesRequest(
    TemplateType? Type = null,
    string? SearchTerm = null,
    string? SortBy = "name_asc") : PagedRequest
{
    public GetTemplatesRequest(
        TemplateType? type,
        string? searchTerm,
        string? sortBy,
        int pageNumber,
        int pageSize) : this(type, searchTerm, sortBy)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
