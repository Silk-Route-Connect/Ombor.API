using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Builds stock-movement ledgers by deriving them on read from every stock event (opening, supply, sale,
/// refund, adjustment, transfer). Nothing is stored; the running balance reconciles to the live stock.
/// </summary>
public interface IMovementService
{
    /// <summary>A warehouse's movements, newest-first, with the running per-product balance.</summary>
    /// <exception cref="EntityNotFoundException{Warehouse}">If no warehouse with the given ID exists.</exception>
    Task<WarehouseMovementDto[]> GetWarehouseMovementsAsync(int warehouseId);

    /// <summary>A product's movements across warehouses, newest-first, with the running total-stock balance.</summary>
    /// <exception cref="EntityNotFoundException{Product}">If no product with the given ID exists.</exception>
    Task<ProductMovementDto[]> GetProductMovementsAsync(int productId);
}
