using Bogus;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class InventoryBuilder(Faker faker) : BuilderBase(faker), IInventoryBuilder
{
    private int? _id;
    private string? _name;
    private string? _location;
    private bool? _isActive;
    private List<InventoryItem>? _inventoryItems;

    public IInventoryBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IInventoryBuilder WithName(string? name = null)
    {
        _name = name ?? $"{_faker.Random.Word()} Inventory";

        return this;
    }

    public IInventoryBuilder WithLocation(string? location = null)
    {
        _location = location ?? _faker.Address.City();

        return this;
    }

    public IInventoryBuilder WithIsAction(bool? isActive = null)
    {
        _isActive = isActive ?? _faker.Random.Bool();

        return this;
    }

    public IInventoryBuilder WithInventoryItems(IEnumerable<InventoryItem>? inventoryItems = null)
    {
        var inventoryIdForItems = _id ?? _faker.Random.Number();
        var productIdForItems = _id ?? _faker.Random.Number();

        _inventoryItems = inventoryItems is null
            ? InventoryItemGenerator.Generate(productIdForItems, inventoryIdForItems, 10)
            : [.. inventoryItems];

        return this;
    }

    public Inventory Build() =>
        new()
        {
            Id = _id ?? default,
            Name = _name ?? string.Empty,
            Location = _location ?? string.Empty,
            IsActive = _isActive ?? default,
            InventoryItems = _inventoryItems ?? []
        };

    public Inventory BuildAndPopulate()
    {
        var inventoryId = _id ?? _faker.Random.Number();
        var productId = _id ?? _faker.Random.Number();

        var inventoryItems = _inventoryItems ?? InventoryItemGenerator.Generate(productId, inventoryId, 10);

        return new()
        {
            Id = inventoryId,
            Name = _name ?? $"{_faker.Random.Word()} Inventory",
            Location = _location ?? _faker.Address.City(),
            IsActive = _isActive ?? _faker.Random.Bool(),
            InventoryItems = inventoryItems
        };
    }
}
