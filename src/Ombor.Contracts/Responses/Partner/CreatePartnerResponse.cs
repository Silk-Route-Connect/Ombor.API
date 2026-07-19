namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// Response returned after successfully creating a partner.
/// </summary>
/// <param name="Id">The newly created partner's ID.</param>
/// <param name="Name">The partner's name.</param>
/// <param name="Type">The type of the partner.</param>
/// <param name="Address">The partner's address, if any.</param>
/// <param name="Email">The partner's Email, if any.</param>
/// <param name="CompanyName">The partner's Company name, if any.</param>
/// <param name="OpeningBalance">The immutable opening balance recorded at creation.</param>
/// <param name="OpeningDate">The date the opening balance was recorded.</param>
/// <param name="IsArchived">Whether the partner is archived.</param>
/// <param name="PhoneNumbers">Phone numbers of partner.</param>
/// <param name="Telegram">The partner's Telegram handle, if any.</param>
public sealed record CreatePartnerResponse(
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
