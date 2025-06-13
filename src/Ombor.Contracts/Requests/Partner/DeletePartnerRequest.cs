namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Data required to delete an existing partner
/// </summary>
/// <param name="id">The indentifier of the product to delete.</param>
public sealed record DeletePartnerRequest(int Id);