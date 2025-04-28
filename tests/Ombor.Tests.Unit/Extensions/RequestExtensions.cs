using Ombor.Contracts.Requests.Product;

namespace Ombor.Tests.Unit.Extensions;
internal static class RequestExtensions
{
    public static bool IsEmpty(this GetProductsRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm) &&
        !request.CategoryId.HasValue &&
        !request.MinPrice.HasValue &&
        !request.MaxPrice.HasValue;

    public static bool IsFullyPopulated(this GetProductsRequest request) =>
        !string.IsNullOrWhiteSpace(request.SearchTerm) &&
        request.CategoryId.HasValue &&
        request.MinPrice.HasValue &&
        request.MaxPrice.HasValue;
}
