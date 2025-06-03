namespace Ombor.Contracts.Responses.Supplier;

/// <summary>
/// DTO representing a supplier for client consumption
/// </summary>
/// <param name="Id">The supplier ID</param>
/// <param name="Name">The supplier name</param>
/// <param name="Address">The supplier address if any</param>
/// <param name="Email">The supplier Email if any</param>
/// <param name="CompanyName">The supplier company name if any</param>
/// <param name="IsActive">Status of supplier</param>
/// <param name="PhoneNumbers">Phone numbers of supplier</param>
public sealed record SupplierDto(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    List<string> PhoneNumbers
);
