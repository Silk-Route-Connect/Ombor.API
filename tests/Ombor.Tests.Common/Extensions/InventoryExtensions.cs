using Ombor.Contracts.Requests.Inventory;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class InventoryExtensions
{
    public static bool IsEquivalent(this Inventory inventory, CreateInventoryRequest request) =>
        inventory.Name == request.Name &&
        inventory.Location == request.Location &&
        inventory.IsActive == request.IsActive;
}
