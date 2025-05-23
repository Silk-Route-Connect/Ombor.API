namespace Ombor.Contracts.Requests.Supplier;

/// <summary>
/// Request to retrieve a list of suppliers with optional filtering
/// </summary>
/// <param name="SearchTerm">
///  Optional case-insensitive term to filter by Name, Address, Email, CompanyName
/// </param>
public sealed record GetSuppliersRequest(
    string? SearchTerm
);