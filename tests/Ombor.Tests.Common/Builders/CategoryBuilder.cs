using Bogus;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class CategoryBuilder(Faker faker) : BuilderBase(faker), ICategoryBuilder
{
    private int? _id;
    private string? _name;
    private string? _description;
    private Product[]? _products;

    public ICategoryBuilder WithId(int? id = null)
    {
        _id = id;

        return this;
    }

    public ICategoryBuilder WithName(string? name)
    {
        _name = name ?? _faker.Commerce.Categories(1)[0];

        return this;
    }

    public ICategoryBuilder WithDescription(string? description)
    {
        _description = description ?? _faker.Lorem.Sentence(1);

        return this;
    }

    public ICategoryBuilder WithProducts(IEnumerable<Product>? products = null)
    {
        var categoryIdForProducts = _id ?? 1;

        _products = products is null
            ? [.. ProductGenerator.Generate([categoryIdForProducts], 5)]
            : [.. products];

        return this;
    }

    public Category Build() =>
        new()
        {
            Id = _id ?? _faker.Random.Number(),
            Name = _name ?? _faker.Commerce.Categories(1)[0],
            Description = _description,
            Products = _products ?? []
        };

    public Category BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();

        return new()
        {
            Id = id,
            Name = _name ?? _faker.Commerce.Categories(1)[0],
            Description = _description ?? _faker.Lorem.Sentence(1),
            Products = _products ?? [.. ProductGenerator.Generate([id], 5)]
        };
    }
}
