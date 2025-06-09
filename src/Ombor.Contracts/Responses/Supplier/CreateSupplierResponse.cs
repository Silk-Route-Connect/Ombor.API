namespace Ombor.Contracts.Responses.Supplier;

/// <summary>
/// Response returned after successfully creating a supplier
/// </summary>
/// <param name="Id">The newly created supplier's ID</param>
/// <param name="Name">The supplier's name</param>
/// <param name="Address">The supplier's address, if any</param>
/// <param name="Email">The supplier's Email, if any</param>
/// <param name="CompanyName">The supplier's Company name, if any</param>
/// <param name="IsActive">Status of newly created supplier</param>
/// <param name="Balance">Balance of newly created supplier</param>
/// <param name="PhoneNumbers">Phone numbers of suppler</param>
public sealed record CreateSupplierResponse(
    int Id,
    string Name,
    string? Address,
    string? Email,
    string? CompanyName,
    bool IsActive,
    decimal Balance,
    List<string> PhoneNumbers
);