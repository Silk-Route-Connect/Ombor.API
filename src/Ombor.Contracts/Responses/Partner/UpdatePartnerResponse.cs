namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// Response returned after successfully updating a partner.
/// </summary>
/// <param name="Id">The partner ID.</param>
/// <param name="Name">The updated name.</param>
/// <param name="Type">The type of updated partner.</param>
/// <param name="Address">The updated address, if any.</param>
/// <param name="Email">The updated Email, if any.</param>
/// <param name="CompanyName">The updated Company name, if any.</param>
/// <param name="OpeningBalance">The immutable opening balance (unchanged by update).</param>
/// <param name="OpeningDate">The date the opening balance was recorded.</param>
/// <param name="IsArchived">Whether the partner is archived.</param>
/// <param name="PhoneNumbers">Updated phone numbers.</param>
/// <param name="Telegram">The partner's Telegram handle, if any.</param>
public sealed record UpdatePartnerResponse(
    int Id,
    string Name,
    string Type,
    string? Address,
    string? Email,
    string? CompanyName,
    decimal OpeningBalance,
    DateOnly OpeningDate,
    bool IsArchived,
    List<string> PhoneNumbers,
    string? Telegram = null);
