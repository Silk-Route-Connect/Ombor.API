using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Tests.Common.Factories;

public static class WarehouseRequestFactory
{
    private const int DefaultWarehouseId = 10;

    // Names are unique per call — warehouse names must be unique, and the integration suite shares a DB.
    public static CreateWarehouseRequest GenerateValidCreateRequest()
        => new(
            Name: $"Warehouse {Guid.NewGuid():N}",
            Location: "Boston");

    public static CreateWarehouseRequest GenerateInvalidCreateRequest()
            => new(
                Name: "",
                Location: "");

    public static UpdateWarehouseRequest GenerateValidUpdateRequest(int? warehouseId = null)
            => new(
                Id: warehouseId ?? DefaultWarehouseId,
                Name: $"Updated warehouse {Guid.NewGuid():N}",
                Location: "Warehouse location");

    public static UpdateWarehouseRequest GenerateInvalidUpdateRequest(int? warehouseId = null)
            => new(
                Id: warehouseId ?? DefaultWarehouseId,
                Name: "",
                Location: " location");
}
