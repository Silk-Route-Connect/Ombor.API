using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
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
    /// Hard-deletes a warehouse that has no referential history. Referenced warehouses are archive-only
    /// (rule 32) and rejected with a 409 instead.
    /// </summary>
    /// <exception cref="ValidationException">If validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    /// <exception cref="ConflictException">If the warehouse is referenced by stock, movements, transfers, transactions, or orders.</exception>
    Task DeleteAsync(DeleteWarehouseRequest request);

    /// <summary>
    /// Sets or clears (null) the low-stock threshold of one product's stock row in a warehouse (DR-41) and returns the
    /// row. A setting, not a stock movement: quantity and cost are untouched.
    /// </summary>
    /// <exception cref="ValidationException">If the threshold is negative or does not fit a stock quantity.</exception>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    /// <exception cref="EntityNotFoundException{WarehouseItem}">If the product has no stock row in the warehouse.</exception>
    Task<WarehouseStockItemDto> SetLowStockThresholdAsync(int warehouseId, int productId, SetLowStockThresholdRequest request);

    /// <summary>
    /// Records the opening (initial) stock of a warehouse as an auditable stock-in event, creating a
    /// warehouse item per product with its initial weighted-average cost and optional low-stock threshold.
    /// </summary>
    /// <exception cref="ValidationException">If validation fails or a product is already stocked.</exception>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseDto> AddOpeningStockAsync(AddOpeningStockRequest request);
}
