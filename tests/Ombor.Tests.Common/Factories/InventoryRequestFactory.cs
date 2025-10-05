using Ombor.Contracts.Requests.Inventory;

namespace Ombor.Tests.Common.Factories;

public static class InventoryRequestFactory
{
    private const int DefaultInventoryId = 10;

    public static CreateInventoryRequest GenerateValidCreateRequest()
        => new(
            Name: "Inventory 123",
            Location: "Boston",
            IsActive: true);

    public static CreateInventoryRequest GenerateInvalidCreateRequest()
            => new(
                Name: "",
                Location: "",
                IsActive: true);

    public static UpdateInventoryRequest GenerateValidUpdateRequest(int? inventoryId = null)
            => new(
                Id: inventoryId ?? DefaultInventoryId,
                Name: "updated inventory",
                Location: "Inventory location",
                IsActive: true);

    public static UpdateInventoryRequest GenerateInvalidUpdateRequest(int? inventoryId = null)
            => new(
                Id: inventoryId ?? DefaultInventoryId,
                Name: "",
                Location: " location",
                IsActive: true);
}
