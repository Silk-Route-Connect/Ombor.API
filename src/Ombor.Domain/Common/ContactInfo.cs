namespace Ombor.Domain.Common;

/// <summary>
/// Represents contact information for an individual or entity.
/// </summary>
public class ContactInfo
{
    /// <summary>
    /// A list of phone numbers associated with the contact.
    /// </summary>
    public required string[] PhoneNumbers { get; set; }

    /// <summary>
    /// An optional email address for the contact.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// An optional physical address for the contact.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// An optional Telegram account username for the contact.
    /// </summary>
    public string? TelegramAccount { get; set; }
}
