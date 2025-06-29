using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

public interface IInventoryMapping
{
    Inventory ToEntity(CreateInventoryRequest request);
    InventoryDto ToDto(Inventory inventory);
    CreateInventoryResponse ToCreateResponse(Inventory inventory);
    UpdateInventoryResponse ToUpdateResponse(Inventory inventory);
    void ApplyUpdate(Inventory inventory, UpdateInventoryRequest request);
}

public class InventoryMappings : IInventoryMapping
{
    public InventoryDto ToDto(Inventory inventory) =>
        new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive);

    public Inventory ToEntity(CreateInventoryRequest request) =>
        new()
        {
            Name = request.Name,
            Location = request.Location,
            IsActive = request.IsActive
        };

    public CreateInventoryResponse ToCreateResponse(Inventory inventory) =>
        new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive);

    public UpdateInventoryResponse ToUpdateResponse(Inventory inventory) =>
        new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive);

    public void ApplyUpdate(Inventory inventory, UpdateInventoryRequest request)
    {
        inventory.Name = request.Name;
        inventory.Location = request.Location;
        inventory.IsActive = request.IsActive;
    }
}
