namespace Ombor.Contracts.Common;

/// <summary>
/// Represents contact information for an individual or entity.
/// </summary>
/// <remarks>
/// This record encapsulates various forms of contact details, including phone numbers, email address,
/// physical address, and a Telegram account. All fields are optional except for the phone numbers,
/// which provides an empty array if no phone numbers are associated.
/// </remarks>
/// <param name="PhoneNumbers">A list of phone numbers.</param>
/// <param name="Email">Email address.</param>
/// <param name="Address">Address.</param>
/// <param name="TelegramAccount">Telegram account.</param>
public sealed record ContactInfo(
    string[] PhoneNumbers,
    string? Email,
    string? Address,
    string? TelegramAccount);
