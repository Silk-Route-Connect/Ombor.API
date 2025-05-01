using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class CategoryAssertionHelper
{
    public static void AssertEquivalent(Category? expected, CreateCategoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    public static void AssertEquivalent(Category? expected, CategoryDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    public static void AssertEquivalent(UpdateCategoryRequest? expected, UpdateCategoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    public static void AssertEquivalent(UpdateCategoryRequest? expected, Category? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }
}
