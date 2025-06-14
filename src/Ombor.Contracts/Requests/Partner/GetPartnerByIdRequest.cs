namespace Ombor.Contracts.Requests.Partner;

/// <summary>
/// Request to retrieve a single partner by their identifier
/// </summary>
/// <param name="Id">The identifier of the partner to fetch.</param>
public sealed record GetPartnerByIdRequest(int Id);
