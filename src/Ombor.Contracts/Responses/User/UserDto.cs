using Ombor.Contracts.Responses.Organization;

namespace Ombor.Contracts.Responses.User;

public sealed record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? TelegramAccount,
    OrganizationDto Organization,
    List<string> Roles,
    List<string> Permissions);
