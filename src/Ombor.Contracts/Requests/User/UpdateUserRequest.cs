namespace Ombor.Contracts.Requests.User;

public sealed record UpdateUserRequest(
    int Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? TelegramAccount,
    List<int>? RoleIds);
