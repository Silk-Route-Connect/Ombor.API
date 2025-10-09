using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Template;

public sealed class GetTemplatesRequest : PagedRequest
{
    public TemplateType? Type { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "name_asc";

    public GetTemplatesRequest() { }

    public GetTemplatesRequest(
        TemplateType? type = null,
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
