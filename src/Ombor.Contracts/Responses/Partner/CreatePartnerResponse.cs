namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// Response returned after successfully creating a partner
/// </summary>
/// <param name="Id">The newly created partner's ID</param>
/// <param name="Name">The partner's name</param>
/// <param name="Address">The partner's address, if any</param>
/// <param name="Email">The partner's Email, if any</param>
/// <param name="CompanyName">The partner's Company name, if any</param>
/// <param name="IsActive">Status of newly created partner</param>
/// <param name="Balance">Balance of newly created partner</param>
/// <param name="PhoneNumbers">Phone numbers of suppler</param>
public sealed record CreatePartnerResponse(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers
);