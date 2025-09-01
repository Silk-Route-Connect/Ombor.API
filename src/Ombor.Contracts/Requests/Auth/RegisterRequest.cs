namespace Ombor.Contracts.Requests.Auth;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Password,
    string ConfirmPassword,
    string PhoneNumber,
    string Email,
    string TelegramAccount);