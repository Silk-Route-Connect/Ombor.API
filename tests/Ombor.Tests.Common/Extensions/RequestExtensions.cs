using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Tests.Common.Extensions;

public static class RequestExtensions
{
    public static bool IsEmpty(this GetCategoriesRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm);

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
