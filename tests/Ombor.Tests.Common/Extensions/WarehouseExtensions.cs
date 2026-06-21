using Ombor.Contracts.Requests.Warehouse;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class WarehouseExtensions
{
    public static bool IsEquivalent(this Warehouse warehouse, CreateWarehouseRequest request) =>
        warehouse.Name == request.Name &&
        warehouse.Location == request.Location;
}
