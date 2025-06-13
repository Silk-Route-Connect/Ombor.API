namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request model to create a new partner.
/// </summary>
/// <param name="Name">The partner's name</param>
/// <param name="Address">An optional partner's address</param>
/// <param name="Email">An optional partner's Email</param>
/// <param name="CompanyName">An optional partner's company name</param>
/// <param name="IsActive">The partner's status</param>
/// <param name="Balance">The partner's balance</param>
/// <param name="PhoneNumbers">The partner's phone numbers</param>
public sealed record CreatePartnerRequest(
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers);