using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Operations for warehouse management. Warehouses are soft-archived, never hard-deleted (rule 31).
/// </summary>
public interface IWarehouseService
{
    /// <summary>Retrieves all warehouses (archived included), optionally filtered by a search term.</summary>
    Task<WarehouseDto[]> GetAsync(GetWarehousesRequest request);

    /// <summary>Retrieves a single warehouse by its identifier.</summary>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> GetByIdAsync(GetWarehouseByIdRequest request);

    /// <summary>Retrieves the products on hand in a warehouse with their warehouse-local WAC and value.</summary>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseStockItemDto[]> GetStockAsync(GetWarehouseByIdRequest request);

    /// <summary>Creates a new (empty) warehouse.</summary>
    /// <exception cref="ValidationException">If validation fails or the name is already taken.</exception>
    Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request);

    /// <summary>Updates an existing warehouse's name/location.</summary>
    /// <exception cref="ValidationException">If validation fails or the name is already taken.</exception>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> UpdateAsync(UpdateWarehouseRequest request);

    /// <summary>Archives a warehouse (soft-delete; still counts in totals).</summary>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> ArchiveAsync(int id);

    /// <summary>Restores an archived warehouse.</summary>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> RestoreAsync(int id);

    /// <summary>
    /// Records the opening (initial) stock of a warehouse as an auditable stock-in event, creating a
    /// warehouse item per product with its initial weighted-average cost.
    /// </summary>
    /// <exception cref="ValidationException">If validation fails or a product is already stocked.</exception>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> AddOpeningStockAsync(AddOpeningStockRequest request);
}
