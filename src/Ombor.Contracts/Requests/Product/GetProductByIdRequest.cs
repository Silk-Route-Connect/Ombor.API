namespace Ombor.Contracts.Requests.Product;

/// <summary>
/// Request to retrieve a single product by its identifier.
/// </summary>
/// <param name="Id">The identifier of the product to fetch. Must be &gt; 0.</param>
public sealed record GetProductByIdRequest(int Id);
