using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators;

namespace Ombor.Tests.Common.Interfaces;

/// <summary>
/// Fluent builder for creating <see cref="Category"/> instances,
/// allowing overrides for any field and two build modes:
/// <list type="bullet">
///   <item>
///     <term><c><see cref="Build"/></c></term>
///     <description> applies explicit overrides and defaults for required fields.</description></item>
///   <item>
///     <term><c><see cref="BuildAndPopulate"/></c></term>
///     <description> applies explicit overrides and random values for all unset properties.</description>
///   </item>
/// </list>
/// </summary>
public interface ICategoryBuilder
{
    /// <summary>
    /// Specifies the <see cref="Category.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The category ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ICategoryBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Category.Name"/>.
    /// If <paramref name="name"/> is <c>null</c>, a random name will be assigned.
    /// </summary>
    /// <param name="name">The category name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ICategoryBuilder WithName(string? name = null);

    /// <summary>
    /// Specifies the <see cref="Category.Description"/>.
    /// If <paramref name="description"/> is <c>null</c>, a random sentence will be assigned.
    /// </summary>
    /// <param name="description">The description, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ICategoryBuilder WithDescription(string? description = null);

    /// <summary>
    /// Specifies the <see cref="Category.Products"/> collection.
    /// If <paramref name="products"/> is <c>null</c>, a list of five products generated via
    /// <see cref="ProductGenerator.Generate(int, int, string)"/> will be assigned.
    /// </summary>
    /// <param name="products">The products to assign, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ICategoryBuilder WithProducts(IEnumerable<Product>? products = null);

    /// <summary>
    /// Builds a <see cref="Category"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Description</c></term><description> null if not set.</description></item>
    ///   <item><term><c>Products</c></term><description> empty list if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A new <see cref="Category"/> populated with only explicitly set and required fields.</returns>
    Category Build();

    /// <summary>
    /// Builds a <see cref="Category"/> and populates all unspecified
    /// properties with random data:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Description</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Products</c></term>
    ///     <description>
    ///         five products generated via <see cref="ProductGenerator.Generate(int, int, string)"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <returns>A fully populated <see cref="Category"/>.</returns>
    Category BuildAndPopulate();
}
