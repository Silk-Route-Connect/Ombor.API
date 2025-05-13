using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Generators;

namespace Ombor.Tests.Common.Interfaces;

/// <summary>
/// Fluent builder for creating <see cref="Product"/> instances,
/// allowing overrides for any field and two build modes:
/// <list type="bullet">
///   <item>
///     <term><c><see cref="Build"/></c></term>
///     <description> applies explicit overrides and defaults for required fields.</description>
///   </item>
///   <item>
///     <term><c><see cref="BuildAndPopulate"/></c></term>
///     <description> applies explicit overrides and random values for all unset properties.</description>
///   </item>
/// </list>
/// </summary>
public interface IProductBuilder
{
    /// <summary>
    /// Specifies the <see cref="Product.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The product ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Product.Name"/>.
    /// If <paramref name="name"/> is <c>null</c>, a random name will be assigned.
    /// </summary>
    /// <param name="name">The product name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithName(string? name = null);

    /// <summary>
    /// Specifies the <see cref="Product.SKU"/>.
    /// If <paramref name="sku"/> is <c>null</c>, a random GUID string will be assigned.
    /// </summary>
    /// <param name="sku">The SKU, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithSKU(string? sku = null);

    /// <summary>
    /// Specifies the <see cref="Product.Description"/>.
    /// If <paramref name="description"/> is <c>null</c>, a random description will be assigned.
    /// </summary>
    /// <param name="description">The description, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithDescription(string? description = null);

    /// <summary>
    /// Specifies the <see cref="Product.Barcode"/>.
    /// If <paramref name="barcode"/> is <c>null</c>, a random EAN-13 barcode will be assigned.
    /// </summary>
    /// <param name="barcode">The barcode, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithBarcode(string? barcode = null);

    /// <summary>
    /// Specifies the <see cref="Product.SalePrice"/>.
    /// If <paramref name="salePrice"/> is <c>null</c>, a random decimal will be assigned.
    /// </summary>
    /// <param name="salePrice">The sale price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithSalePrice(decimal? salePrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.SupplyPrice"/>.
    /// If <paramref name="supplyPrice"/> is <c>null</c>, a random decimal will be assigned.
    /// </summary>
    /// <param name="supplyPrice">The supply price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithSupplyPrice(decimal? supplyPrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.RetailPrice"/>.
    /// If <paramref name="retailPrice"/> is <c>null</c>, a random decimal will be assigned.
    /// </summary>
    /// <param name="retailPrice">The retail price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithRetailPrice(decimal? retailPrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.QuantityInStock"/>.
    /// If <paramref name="quantityInStock"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="quantityInStock">The quantity in stock, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithQuantityInStock(int? quantityInStock = null);

    /// <summary>
    /// Specifies the <see cref="Product.LowStockThreshold"/>.
    /// If <paramref name="lowStockThreshold"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="lowStockThreshold">The low stock threshold, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithLowStockThreshold(int? lowStockThreshold = null);

    /// <summary>
    /// Specifies the <see cref="Product.Measurement"/>.
    /// If <paramref name="measurement"/> is <c>null</c>, <see cref="UnitOfMeasurement.None"/> will be used.
    /// </summary>
    /// <param name="measurement">The unit of measurement, or <c>null</c> to default to None.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithMeasurement(UnitOfMeasurement? measurement = null);

    /// <summary>
    /// Specifies the <see cref="Product.ExpireDate"/>.
    /// If <paramref name="expireDate"/> is <c>null</c>, a future <see cref="DateOnly"/> will be assigned.
    /// </summary>
    /// <param name="expireDate">The expiration date, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithExpireDate(DateOnly? expireDate = null);

    /// <summary>
    /// Specifies the <see cref="Product.Category"/> and <see cref="Product.CategoryId"/>.
    /// If <paramref name="category"/> is <c>null</c>, <see cref="CategoryGenerator.Generate(string)"/> will be used.
    /// </summary>
    /// <param name="category">The category, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithCategory(Category? category = null);

    /// <summary>
    /// Specifies the <see cref="Product.CategoryId"/>.
    /// If <paramref name="categoryId"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="categoryId">The category ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IProductBuilder WithCategoryId(int? categoryId = null);

    /// <summary>
    /// Builds a <see cref="Product"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>SKU</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Description</c></term><description> null if not set.</description></item>
    ///   <item><term><c>Barcode</c></term><description> null if not set.</description></item>
    ///   <item><term><c>SalePrice</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>SupplyPrice</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>RetailPrice</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>QuantityInStock</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>LowStockThreshold</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Measurement</c></term><description> <see cref="UnitOfMeasurement.None"/> if not set.</description></item>
    ///   <item><term><c>ExpireDate</c></term><description> default if not set.</description></item>
    ///   <item><term><c>Category</c></term><description> generated via <see cref="CategoryGenerator.Generate(string)"/> and assigned random number to its <c>ID</c> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A new <see cref="Product"/> populated with only explicitly set and required fields.</returns>
    Product Build();

    /// <summary>
    /// Builds a <see cref="Product"/> and populates all unspecified
    /// properties with random data:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> random if not set.</description></item>
    ///   <item><term><c>SKU</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Description</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Barcode</c></term><description> random if not set.</description></item>
    ///   <item><term><c>SalePrice</c></term><description> random if not set.</description></item>
    ///   <item><term><c>SupplyPrice</c></term><description> random if not set.</description></item>
    ///   <item><term><c>RetailPrice</c></term><description> random if not set.</description></item>
    ///   <item><term><c>QuantityInStock</c></term><description> random if not set.</description></item>
    ///   <item><term><c>LowStockThreshold</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Measurement</c></term><description> random if not set.</description></item>
    ///   <item><term><c>ExpireDate</c></term><description> random if not set.</description></item>
    ///   <item><term><c>Category</c></term><description> generated via <see cref="CategoryGenerator.Generate(string)"/> and assigned random number to its <c>ID</c> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A fully populated <see cref="Product"/>.</returns>
    Product BuildAndPopulate();
}
