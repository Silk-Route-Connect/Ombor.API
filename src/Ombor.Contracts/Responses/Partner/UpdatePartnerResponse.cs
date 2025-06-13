namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// Response returned after successfully updating a partner
/// </summary>
/// <param name="Id">The partner ID</param>
/// <param name="Name">The updated name</param>
/// <param name="Address">The updated address, if any</param>
/// <param name="Email">The updated Email, if any</param>
/// <param name="CompanyName">The updated Company name, if any</param>
/// <param name="IsActive">Status of updated partner</param>
/// <param name="Balance">Balance of updated partner</param>
/// <param name="PhoneNumbers">Updated phone numbers</param>
public sealed record UpdatePartnerResponse(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers);
