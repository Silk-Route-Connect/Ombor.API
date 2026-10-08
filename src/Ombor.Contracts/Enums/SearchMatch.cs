namespace Ombor.Contracts.Enums;

/// <summary>Which field of a record matched a global-search query.</summary>
public enum SearchMatch
{
    /// <summary>The name (partner, product, employee, warehouse, wallet).</summary>
    Name,

    /// <summary>The partner's company name.</summary>
    Company,

    /// <summary>One of the partner's or employee's phone numbers (digits compared).</summary>
    Phone,

    /// <summary>The product's SKU.</summary>
    Sku,

    /// <summary>The product's barcode (one unit).</summary>
    Barcode,

    /// <summary>The barcode of the product's package — a scan of it means one package.</summary>
    PackagingBarcode,

    /// <summary>The document number (exact).</summary>
    Number,
}
