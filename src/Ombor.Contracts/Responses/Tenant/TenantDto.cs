namespace Ombor.Contracts.Responses.Tenant;

public sealed record TenantDto(
    int Id,
    string Name,
    bool IsActive,
    int UserCount,
    int RoleCount);
