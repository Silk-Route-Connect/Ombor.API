using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class CategoryExtensions
{
    public static bool IsEquivalent(this Category category, CreateCategoryRequest request) =>
        category.Name == request.Name &&
        category.Description == request.Description;
}
