namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// DTO representing a partner for client consumption.
/// </summary>
/// <param name="Id">The partner ID.</param>
/// <param name="Name">The partner name.</param>
/// <param name="Type">Type of the partner.</param>
/// <param name="Address">The partner address if any.</param>
/// <param name="Email">The partner Email if any.</param>
/// <param name="CompanyName">The partner company name if any.</param>
/// <param name="PhoneNumbers">Phone numbers of partner.</param>
/// <param name="Balance">Computed net balance (positive = the partner owes us).</param>
/// <param name="OpeningBalance">Immutable opening balance recorded at creation.</param>
/// <param name="OpeningDate">The date the opening balance was recorded.</param>
/// <param name="IsArchived">Whether the partner is archived.</param>
/// <param name="IsDeletable">True when no other record references the partner (otherwise DELETE returns 409).</param>
/// <param name="ActivityCount">Number of records referencing the partner (transactions, payments, orders, templates).</param>
public sealed record PartnerDto(
    int Id,
    string Name,
    string Type,
    string? Address,
    string? Email,
    string? CompanyName,
    List<string> PhoneNumbers,
    decimal Balance,
    decimal OpeningBalance,
    DateOnly OpeningDate,
    bool IsArchived,
    bool IsDeletable,
    int ActivityCount);
