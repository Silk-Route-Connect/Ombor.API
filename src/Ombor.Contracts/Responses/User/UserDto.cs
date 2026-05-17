using Ombor.Contracts.Responses.Tenant;

namespace Ombor.Contracts.Responses.User;

public sealed record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? TelegramAccount,
    TenantDto Tenant,
    List<string> Roles,
    List<string> Permissions);
