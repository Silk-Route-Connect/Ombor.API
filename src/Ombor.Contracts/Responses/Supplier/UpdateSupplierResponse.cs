namespace Ombor.Contracts.Responses.Supplier;

/// <summary>
/// Response returned after successfully updating a supplier
/// </summary>
/// <param name="Id">The supplier ID</param>
/// <param name="Name">The updated name</param>
/// <param name="Address">The updated address, if any</param>
/// <param name="Email">The updated Email, if any</param>
/// <param name="CompanyName">The updated Company name, if any</param>
/// <param name="IsActive">Status of updated supplier</param>
/// <param name="Balance">Balance of updated supplier</param>
/// <param name="PhoneNumbers">Updated phone numbers</param>
public sealed record UpdateSupplierResponse(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers
);