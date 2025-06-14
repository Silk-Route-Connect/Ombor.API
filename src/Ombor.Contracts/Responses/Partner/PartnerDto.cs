namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// DTO representing a partner for client consumption
/// </summary>
/// <param name="Id">The partner ID</param>
/// <param name="Name">The partner name</param>
/// <param name="Type">Type of the partner</param>
/// <param name="Address">The partner address if any</param>
/// <param name="Email">The partner Email if any</param>
/// <param name="CompanyName">The partner company name if any</param>
/// <param name="Balance">Balance of partner</param>
/// <param name="PhoneNumbers">Phone numbers of partner</param>
public sealed record PartnerDto(
    int Id,
    string Name,
    string Type,
    string? Address,
    string? Email,
    string? CompanyName,
    decimal Balance,
    List<string> PhoneNumbers);
