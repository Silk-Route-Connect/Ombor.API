namespace Ombor.Contracts.Requests.Auth;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    string OrganizationName,
    string? Email,
    string? TelegramAccount);
