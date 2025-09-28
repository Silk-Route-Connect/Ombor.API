using FluentValidation;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defines CRUD operations for inventory management.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Retrieves all inventories, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">Filtering parameter for fetching inventories.</param>
    /// <returns>An array of <see cref="InventoryDto"/> Matching the filter.</returns>
    Task<InventoryDto[]> GetAsync(GetInventoriesRequest request);

    /// <summary>
    /// Retrieves a single inventory by its identifier.
    /// </summary>
    /// <param name="request">Contains the <see cref="GetInventoryByIdRequest"/> of the inventory to fetch.</param>
    /// <returns>The matching <see cref="InventoryDto"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{inventory}">If no inventory with the given ID exists.</exception>
    Task<InventoryDto> GetByIdAsync(GetInventoryByIdRequest request);

    /// <summary>
    /// Creates a new inventory.
    /// </summary>
    /// <param name="request">The properties of the inventory to create.</param>
    /// <returns>Details of the newly created inventory.</returns>
    /// <exception cref="ValidationException"> If <paramref name="request"/> fails validation. </exception>
    Task<CreateInventoryResponse> CreateAsync(CreateInventoryRequest request);

    /// <summary>
    /// Updates an existing inventory.
    /// </summary>
    /// <param name="request">Contains the ID and new values for the inventory.</param>
    /// <returns>Details of the updated inventory.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{partner}">If no inventory with the given ID exists.</exception>
    Task<UpdateInventoryResponse> UpdateAsync(UpdateInventoryRequest request);

    /// <summary>
    /// Deletes an existing inventory by its ID.
    /// </summary>
    /// <param name="request">Contains the <see cref="DeleteInventoryRequest.Id"/> to remove.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{partner}">If no inventory with the given ID exists.</exception>
    Task DeleteAsync(DeleteInventoryRequest request);
}
