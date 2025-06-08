namespace Ombor.Contracts.Requests.Supplier;

/// <summary>
/// Request model to create a new supplier.
/// </summary>
/// <param name="Name">The supplier's name</param>
/// <param name="Address">An optional supplier's address</param>
/// <param name="Email">An optional supplier's Email</param>
/// <param name="CompanyName">An optional supplier's company name</param>
/// <param name="IsActive">The supplier's status</param>
/// <param name="Balance">The supplier's balance</param>
/// <param name="PhoneNumbers">The supplier's phone numbers</param>
public sealed record CreateSupplierRequest(
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers);