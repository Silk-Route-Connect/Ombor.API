namespace Ombor.Contracts.Requests.User;

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? TelegramAccount,
    List<int>? RoleIds);