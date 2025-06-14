namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request to retrieve a list of partners with optional filtering
/// </summary>
/// <param name="SearchTerm">
///  Optional case-insensitive term to filter by Name, Address, Email, CompanyName
/// </param>
public sealed record GetPartnersRequest(string? SearchTerm);