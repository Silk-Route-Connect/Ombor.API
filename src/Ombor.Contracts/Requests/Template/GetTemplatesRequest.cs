using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Template;

public sealed record GetTemplatesRequest(
    TemplateType? Type = null,
    string? SearchTerm = null,
    string? SortBy = "name_asc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);
