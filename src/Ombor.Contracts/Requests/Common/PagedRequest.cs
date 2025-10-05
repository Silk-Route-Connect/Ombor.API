using System.ComponentModel.DataAnnotations;

namespace Ombor.Contracts.Requests.Common;

public sealed record PagedRequest(
    [Range(1,int.MaxValue)]
    int PageNumber = 1,
    [Range(1,100)]
    int PageSize = 10,
    string? SearchTerm = null,
    [MaxLength(255)]
    string? SortBy = null,
    bool SortByDescending = false,
    int? OrganizationId = null);
