using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Extensions;

public static class ProductExtensions
{
    public static bool IsEquivalent(this Product product, CreateProductRequest request) =>
        product.Name == request.Name &&
        product.SKU == request.SKU &&
        product.Description == request.Description &&
        product.Barcode == request.Barcode &&
        product.SalePrice == request.SalePrice &&
        product.SupplyPrice == request.SupplyPrice &&
        product.RetailPrice == request.RetailPrice &&
        product.QuantityInStock == request.QuantityInStock &&
        product.LowStockThreshold == request.LowStockThreshold &&
        IsMeasurementEqual(product.Measurement, request.Measurement) &&
        IsTypeEqual(product.Type, request.Type) &&
        product.CategoryId == request.CategoryId;

    private static bool IsMeasurementEqual(UnitOfMeasurement mappedMeasurement, Contracts.Enums.UnitOfMeasurement requestMeasurement)
        => (int)mappedMeasurement == (int)requestMeasurement;

    private static bool IsTypeEqual(ProductType mappedType, Contracts.Enums.ProductType requestType)
        => (int)mappedType == (int)requestType;
}
