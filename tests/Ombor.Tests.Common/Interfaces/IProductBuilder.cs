using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Interfaces;

/// <summary>
/// Fluent builder for creating <see cref="Product"/> instances,
/// allowing overrides for any property and two build modes:
/// <see cref="Build"/> (uses only required + explicitly set values, CLR defaults for others)
/// or <see cref="BuildAndPopulate"/> (fills all unspecified fields with random data).
/// </summary>
public interface IProductBuilder
{
    /// <summary>
    /// Specifies the <see cref="Product.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The product ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Product.Name"/>.
    /// If <paramref name="name"/> is <c>null</c> or empty, a random product name will be generated.
    /// </summary>
    /// <param name="name">The name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithName(string? name = null);

    /// <summary>
    /// Specifies the <see cref="Product.SKU"/>.
    /// If <paramref name="sku"/> is <c>null</c> or empty, a random GUID string will be used.
    /// </summary>
    /// <param name="sku">The SKU, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithSKU(string? sku = null);

    /// <summary>
    /// Specifies the <see cref="Product.Description"/>.
    /// If <paramref name="description"/> is <c>null</c>, a random product description will be generated.
    /// </summary>
    /// <param name="description">The description, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithDescription(string? description = null);

    /// <summary>
    /// Specifies the <see cref="Product.Barcode"/>.
    /// If <paramref name="barcode"/> is <c>null</c>, a random EAN-13 barcode will be generated.
    /// </summary>
    /// <param name="barcode">The barcode, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithBarcode(string? barcode = null);

    /// <summary>
    /// Specifies the <see cref="Product.SalePrice"/>.
    /// If <paramref name="salePrice"/> is <c>null</c>, a random decimal between
    /// <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and <see cref="Configurations.BuilderConstants.MaxPriceAmount"/>.
    /// </summary>
    /// <param name="salePrice">The sale price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithSalePrice(decimal? salePrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.SupplyPrice"/>.
    /// If <paramref name="supplyPrice"/> is <c>null</c>, a random decimal
    /// <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and <see cref="Configurations.BuilderConstants.MaxPriceAmount"/>.
    /// </summary>
    /// <param name="supplyPrice">The supply price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithSupplyPrice(decimal? supplyPrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.RetailPrice"/>.
    /// If <paramref name="retailPrice"/> is <c>null</c>, a random decimal between
    /// <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and <see cref="Configurations.BuilderConstants.MaxPriceAmount"/>.
    /// </summary>
    /// <param name="retailPrice">The retail price, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithRetailPrice(decimal? retailPrice = null);

    /// <summary>
    /// Specifies the <see cref="Product.QuantityInStock"/>.
    /// If <paramref name="quantityInStock"/> is <c>null</c>, a random integer between
    /// <see cref="Configurations.BuilderConstants.MinStockAmount"/> and <see cref="Configurations.BuilderConstants.MaxStockAmount"/>.
    /// </summary>
    /// <param name="quantityInStock">The stock quantity, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithQuantityInStock(int? quantityInStock = null);

    /// <summary>
    /// Specifies the <see cref="Product.LowStockThreshold"/>.
    /// If <paramref name="lowStockThreshold"/> is <c>null</c>, a random integer between
    /// <see cref="Configurations.BuilderConstants.MinLowThresholdAmount"/> and <see cref="Configurations.BuilderConstants.MaxLowThresholdAmount"/>.
    /// </summary>
    /// <param name="lowStockThreshold">The low-stock threshold, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithLowStockThreshold(int? lowStockThreshold = null);

    /// <summary>
    /// Specifies the <see cref="Product.Measurement"/> unit.
    /// If <paramref name="measurement"/> is <c>null</c>, a random <see cref="UnitOfMeasurement"/> value will be selected.
    /// </summary>
    /// <param name="measurement">The measurement unit, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithMeasurement(UnitOfMeasurement? measurement = null);

    /// <summary>
    /// Specifies the <see cref="Product.ExpireDate"/>.
    /// If <paramref name="expireDate"/> is <c>null</c>, a random future <see cref="DateOnly"/> will be generated.
    /// </summary>
    /// <param name="expireDate">The expiration date, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithExpireDate(DateOnly? expireDate = null);

    /// <summary>
    /// Specifies the <see cref="Product.Category"/> (and underlying <see cref="Product.CategoryId"/>).
    /// If <paramref name="category"/> is <c>null</c>, a random <see cref="Category"/>
    /// (with its own random <c>Id</c>) will be generated.
    /// </summary>
    /// <param name="category">The category instance, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithCategory(Category? category = null);

    /// <summary>
    /// Specifies the <see cref="Product.CategoryId"/>.
    /// If <paramref name="categoryId"/> is <c>null</c>, a random integer will be assigned./>
    /// </summary>
    /// <param name="categoryId">The ID of the category, or <c>null</c> to generate a random one.</param>
    /// <returns>The same <see cref="IProductBuilder"/> instance for chaining.</returns>
    IProductBuilder WithCategoryId(int? categoryId = null);

    /// <summary>
    /// Constructs a <see cref="Product"/> using only explicitly set values.
    /// Any properties not set will use minimal defaults:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> CLR default if not set</description></item>
    ///   <item><term><c>Name</c></term><description> random product name if not set</description></item>
    ///   <item><term><c>SKU</c></term><description> random GUID string if not set</description></item>
    ///   <item><term><c>Description</c></term><description> <c>null</c> if not set</description></item>
    ///   <item><term><c>Barcode</c></term><description> <c>null</c> if not set</description></item>
    ///   <item><term><c>SalePrice</c></term><description> <c>0</c> if not set</description></item>
    ///   <item><term><c>SupplyPrice</c></term><description> <c>0</c> if not set</description></item>
    ///   <item><term><c>RetailPrice</c></term><description> <c>0</c> if not set</description></item>
    ///   <item><term><c>QuantityInStock</c></term><description> <c>0</c> if not set</description></item>
    ///   <item><term><c>LowStockThreshold</c></term><description> <c>0</c> if not set</description></item>
    ///   <item><term><c>Measurement</c></term><description><see cref="UnitOfMeasurement.None"/> if not set</description></item>
    ///   <item><term><c>ExpireDate</c></term><description><c>null</c> if not set</description></item>
    ///   <item><term><c>Category</c></term><description> random if not set</description></item>
    /// </list>
    /// Required overrides (<c>ID</c>, <c>Name</c>, <c>SKU</c>, <c>Category</c>) must be set explicitly or
    /// random defaults will be generated for them.
    /// </summary>
    /// <returns>A <see cref="Product"/> instance with specified and minimal default values.</returns>
    Product Build();

    /// <summary>
    /// Constructs a <see cref="Product"/> and populates all unspecified properties with random data:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> random integer if not set</description></item>
    ///   <item><term><c>Name</c></term><description> random product name if not set</description></item>
    ///   <item><term><c>SKU</c></term><description> random GUID string if not set</description></item>
    ///   <item><term><c>Description</c></term><description> random lorem sentence if not set</description></item>
    ///   <item><term><c>Barcode</c></term><description> random EAN-13 if not set</description></item>
    ///   <item><term><c>SalePrice</c></term>
    ///     <description>
    ///         random between <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and
    ///         <see cref="Configurations.BuilderConstants.MaxPriceAmount"/> if not set
    ///     </description>
    ///   </item>
    ///   <item><term><c>SupplyPrice</c></term>
    ///     <description>
    ///         random between <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and
    ///         <see cref="Configurations.BuilderConstants.MaxPriceAmount"/> if not set
    ///     </description>
    ///   </item>
    ///   <item><term><c>RetailPrice</c></term>
    ///     <description>
    ///         random between <see cref="Configurations.BuilderConstants.MinPriceAmount"/> and
    ///         <see cref="Configurations.BuilderConstants.MaxPriceAmount"/> if not set
    ///     </description>
    ///   </item>
    ///   <item><term><c>QuantityInStock</c></term>
    ///     <description>
    ///         random between <see cref="Configurations.BuilderConstants.MinStockAmount"/> and
    ///         <see cref="Configurations.BuilderConstants.MaxStockAmount"/> if not set
    ///     </description>
    ///   </item>
    ///   <item><term><c>LowStockThreshold</c></term>
    ///     <description>
    ///         random between <see cref="Configurations.BuilderConstants.MinLowThresholdAmount"/> and
    ///         <see cref="Configurations.BuilderConstants.MaxLowThresholdAmount"/> if not set
    ///     </description>
    ///   </item>
    ///   <item><term><c>Measurement</c></term><description> random <see cref="UnitOfMeasurement"/> if not set</description></item>
    ///   <item><term><c>ExpireDate</c></term><description> random future date if not set</description></item>
    ///   <item><term><c>Category</c></term><description> Explicit or randomly generated</description></item>
    /// </list>
    /// Explicit overrides (via <c>With…</c> calls) are always honored.
    /// </summary>
    /// <returns>A fully populated <see cref="Product"/> instance ready for use.</returns>
    Product BuildAndPopulate();
}
