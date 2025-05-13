using Bogus;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Extensions;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

/// <summary>
/// <inheritdoc/>
/// </summary>
internal sealed class CategoryBuilder(Faker faker) : BuilderBase(faker), ICategoryBuilder
{
    private int? _id;
    private string? _name;
    private string? _description;
    private List<Product>? _products;

    public ICategoryBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public ICategoryBuilder WithName(string? name = null)
    {
        _name = name ?? _faker.Commerce.CategoryName();

        return this;
    }

    public ICategoryBuilder WithDescription(string? description = null)
    {
        _description = description ?? _faker.Lorem.Sentence();

        return this;
    }

    public ICategoryBuilder WithProducts(IEnumerable<Product>? products = null)
    {
        var categoryIdForProducts = _id ?? _faker.Random.Number();

        _products = products is null
            ? ProductGenerator.Generate(categoryIdForProducts, 5)
            : [.. products];

        return this;
    }

    public Category Build() =>
        new()
        {
            Id = _id ?? default,
            Name = _name ?? string.Empty,
            Description = _description,
            Products = _products ?? [],
        };

    public Category BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();
        var products = _products ?? ProductGenerator.Generate(id, 5);

        return new()
        {
            Id = id,
            Name = _name ?? _faker.Commerce.CategoryName(),
            Description = _description ?? _faker.Lorem.Sentence(),
            Products = products
        };
    }
}
