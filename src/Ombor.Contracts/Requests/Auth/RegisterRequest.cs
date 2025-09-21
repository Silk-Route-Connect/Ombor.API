namespace Ombor.Contracts.Requests.Auth;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Email,
    string TelegramAccount,
    string Password,
    string ConfirmPassword,
    string? OrganizationName);