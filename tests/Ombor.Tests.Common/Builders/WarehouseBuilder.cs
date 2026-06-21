using Bogus;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class WarehouseBuilder(Faker faker) : BuilderBase(faker), IWarehouseBuilder
{
    private int? _id;
    private string? _name;
    private string? _location;
    private bool? _isArchived;
    private List<WarehouseItem>? _warehouseItems;

    public IWarehouseBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IWarehouseBuilder WithName(string? name = null)
    {
        _name = name ?? $"{_faker.Random.Word()} Warehouse";

        return this;
    }

    public IWarehouseBuilder WithLocation(string? location = null)
    {
        _location = location ?? _faker.Address.City();

        return this;
    }

    public IWarehouseBuilder WithIsArchived(bool? isArchived = null)
    {
        _isArchived = isArchived ?? _faker.Random.Bool();

        return this;
    }

    public IWarehouseBuilder WithWarehouseItems(IEnumerable<WarehouseItem>? warehouseItems = null)
    {
        var warehouseIdForItems = _id ?? _faker.Random.Number();
        var productIdForItems = _faker.Random.Number();

        _warehouseItems = warehouseItems is null
            ? WarehouseItemGenerator.Generate(productIdForItems, warehouseIdForItems, 10)
            : [.. warehouseItems];

        return this;
    }

    public Warehouse Build() =>
        new()
        {
            Id = _id ?? default,
            Name = _name ?? string.Empty,
            Location = _location ?? string.Empty,
            IsArchived = _isArchived ?? default,
            WarehouseItems = _warehouseItems ?? []
        };

    public Warehouse BuildAndPopulate()
    {
        var warehouseId = _id ?? _faker.Random.Number();
        var productId = _faker.Random.Number();

        var warehouseItems = _warehouseItems ?? WarehouseItemGenerator.Generate(productId, warehouseId, 10);

        return new()
        {
            Id = warehouseId,
            Name = _name ?? $"{_faker.Random.Word()} Warehouse",
            Location = _location ?? _faker.Address.City(),
            IsArchived = _isArchived ?? _faker.Random.Bool(),
            WarehouseItems = warehouseItems
        };
    }
}
