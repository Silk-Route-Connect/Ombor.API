namespace Ombor.Contracts.Requests.Supplier;

/// <summary>
/// Data required to delete an existing supplier
/// </summary>
/// <param name="id">The indentifier of the product to delete.</param>
public sealed record DeleteSupplierRequest(int Id);