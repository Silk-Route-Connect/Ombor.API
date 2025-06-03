namespace Ombor.Contracts.Requests.Supplier;

/// <summary>
/// Request to retrieve a single supplier by their identifier
/// </summary>
/// <param name="Id">The identifier of the supplier to fetch.</param>
public sealed record GetSupplierByIdRequest(int Id);
