using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request to update an existing partner
/// </summary>
/// <param name="Id">The identifier of the partner to update.</param>
/// <param name="Name">The new partner name.</param>
/// <param name="Address">An optional new address.</param>
/// <param name="Email">An optional new Email.</param>
/// <param name="CompanyName">An optional new company name.</param>
/// <param name="Balance">The new balance of partner.</param>
/// <param name="Type">The type of the partner.</param>
/// <param name="PhoneNumbers">New phone numbers of partner.</param>
public sealed record UpdatePartnerRequest(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    decimal Balance,
    PartnerType Type,
    List<string> PhoneNumbers);