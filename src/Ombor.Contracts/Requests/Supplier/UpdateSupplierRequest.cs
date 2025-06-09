namespace Ombor.Contracts.Requests.Supplier;

/// <summary>
/// Request to update an existing supplier
/// </summary>
/// <param name="Id">The identifier of the supplier to update</param>
/// <param name="Name">The new supplier name</param>
/// <param name="Address">An optional new address</param>
/// <param name="Email">An optional new Email</param>
/// <param name="CompanyName">An optional new company name</param>
/// <param name="IsActive">The new status of supplier</param>
/// <param name="Balance">The new balance of supplier</param>
/// <param name="PhoneNumbers">New phone numbers of supplier</param>
public sealed record UpdateSupplierRequest(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers);