using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defunes CRUD operations for supplier management.
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Retrueves all suppliers, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">
    /// The filtering options. <see cref="GetSuppliersRequest.SearchTerm"/> may be null or whitespace.
    /// </param>
    /// <returns>An array of <see cref="SupplierDto"/>Matching the filter.</returns>
    Task<SupplierDto[]> GetAsync(GetSuppliersRequest request);

    /// <summary>
    /// Retrieves a single supplier by its identifier.
    /// </summary>
    /// <param name="request">Contains the <see cref="GetSupplierByIdRequest.Id"/> of the supplier to fetch.</param>
    /// <returns>The matching <see cref="SupplierDto"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Supplier}">If no supplier with the given ID exists.</exception>
    Task<SupplierDto> GetByIdAsync(GetSupplierByIdRequest request);

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="request">The properties of the supplier to create.</param>
    /// <returns>Details of the newly created supplier.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    Task<CreateSupplierResponse> CreateAsync(CreateSupplierRequest request);

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    /// <param name="request">Contains the ID and new values for the supplier.</param>
    /// <returns>Details of the updated supplier.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Supplier}">If no supplier with the given ID exists.</exception>
    Task<UpdateSupplierResponse> UpdateAsync(UpdateSupplierRequest request);

    /// <summary>
    /// Deletes an existing supplier by its ID.
    /// </summary>
    /// <param name="request">Contains the <see cref="DeleteSupplierRequest.Id"/> to remove.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Supplier}">If no supplier with the given ID exists.</exception>
    Task DeleteAsync(DeleteSupplierRequest request);
}
