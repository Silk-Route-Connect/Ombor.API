using Ombor.Contracts.Requests.Category;

namespace Ombor.Tests.Common.Factories;

public static class CategoryRequestFactory
{
    private const int DefaultCategoryId = 10;

    public static CreateCategoryRequest GenerateValidCreateRequest()
        => new(
            Name: "Test Category",
            Description: "Category Description");

    public static CreateCategoryRequest GenerateInvalidCreateRequest()
        => new(
            Name: "", // Invalid Name
            Description: "");

    public static UpdateCategoryRequest GenerateValidUpdateRequest(int? categoryId = null)
        => new(
            Id: categoryId ?? DefaultCategoryId,
            Name: "Test Category",
            Description: "Category Description");

    public static UpdateCategoryRequest GenerateInvalidUpdateRequest(int? categoryId = null)
        => new(
            Id: categoryId ?? DefaultCategoryId,
            Name: "", // Invalid Name
            Description: "Category Description");
}
