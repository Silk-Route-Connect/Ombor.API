using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;

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
        product.Measurement.ToString() == request.Measurement &&
        product.ExpireDate == request.ExpireDate &&
        product.CategoryId == request.CategoryId;
}
