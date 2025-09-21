namespace Ombor.Contracts.Responses.Organization;

public sealed record OrganizationDto(
    int Id,
    string Name,
    bool IsActive,
    int UserCount,
    int RoleCount);