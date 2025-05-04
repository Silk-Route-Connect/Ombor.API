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
        product.ExpireDate == request.ExpireDate &&
        product.CategoryId == request.CategoryId;

    private static bool IsMeasurementEqual(UnitOfMeasurement mappedMeasurement, string requestMeasurement)
    {
        if (Enum.TryParse(requestMeasurement, out UnitOfMeasurement result))
        {
            return mappedMeasurement == result;
        }

        return mappedMeasurement == UnitOfMeasurement.None;
    }
}
