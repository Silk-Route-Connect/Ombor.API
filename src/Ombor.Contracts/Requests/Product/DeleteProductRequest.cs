namespace Ombor.Contracts.Requests.Product;

/// <summary>
/// Data required to delete an existing product.
/// </summary>
/// <param name="Id">The identifier of the product to delete. Must be &gt; 0.</param>
public sealed record DeleteProductRequest(int Id);
