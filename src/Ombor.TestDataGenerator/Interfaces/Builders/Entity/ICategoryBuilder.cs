using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Interfaces.Builders.Entity;

/// <summary>
/// Fluent builder for creating <see cref="Category"/> instances,
/// allowing overrides for any field and two build modes:
/// <see cref="Build"/> (only required + explicitly set values)
/// or <see cref="BuildAndPopulate"/> (fills in all unspecified fields with random data).
/// </summary>
public interface ICategoryBuilder
{
    /// <summary>
    /// Specifies the <see cref="Category.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The category ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="ICategoryBuilder"/> instance for chaining.</returns>
    ICategoryBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Category.Name"/>.
    /// If <paramref name="name"/> is <c>null</c> or empty, a random category name will be generated.
    /// </summary>
    /// <param name="name">The name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="ICategoryBuilder"/> instance for chaining.</returns>
    ICategoryBuilder WithName(string? name);

    /// <summary>
    /// Specifies the <see cref="Category.Description"/>.
    /// If <paramref name="description"/> is <c>null</c>, no description will be assigned in <see cref="Build"/>,
    /// but <see cref="BuildAndPopulate"/> will auto-generate a random one.
    /// </summary>
    /// <param name="description">The description, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="ICategoryBuilder"/> instance for chaining.</returns>
    ICategoryBuilder WithDescription(string? description);

    /// <summary>
    /// Specifies the <see cref="Category.Products"/> collection.
    /// If <paramref name="products"/> is <c>null</c>, no products will be assigned in <see cref="Build"/>,
    /// but <see cref="BuildAndPopulate"/> will auto-generate a random set.
    /// </summary>
    /// <param name="products">The products to assign, or <c>null</c> to defer generation.</param>
    /// <returns>The same <see cref="ICategoryBuilder"/> instance for chaining.</returns>
    ICategoryBuilder WithProducts(IEnumerable<Product>? products = null);

    /// <summary>
    /// Constructs a <see cref="Category"/> using only the explicitly set values.
    /// Any properties not set will use defaults:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description>random if not set</description></item>
    ///   <item><term><c>Name</c></term><description>random if not set</description></item>
    ///   <item><term><c>Description</c></term><description>null if not set</description></item>
    ///   <item><term><c>Products</c></term><description>empty if not set</description></item>
    /// </list>
    /// </summary>
    /// <returns>A <see cref="Category"/> instance with required and explicitly set properties.</returns>
    Category Build();

    /// <summary>
    /// Constructs a <see cref="Category"/> and populates <em>all</em> unspecified properties with random data:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description>random if not set</description></item>
    ///   <item><term><c>Name</c></term><description>random if not set</description></item>
    ///   <item><term><c>Description</c></term><description>random if not set</description></item>
    ///   <item><term><c>Products</c></term><description>randomly generated list if not set</description></item>
    /// </list>
    /// Explicit overrides (via <c>With…</c> calls) are always honored.
    /// </summary>
    /// <returns>A fully populated <see cref="Category"/> instance ready for use or persistence.</returns>
    Category BuildAndPopulate();
}
