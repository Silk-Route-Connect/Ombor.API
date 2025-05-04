using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// Provides assertion helper methods for verifying equivalence between domain entities, request DTOs, and response DTOs in xUnit tests for categories.
/// </summary>
public static class CategoryAssertionHelper
{
    /// <summary>
    /// Asserts that a <see cref="Category"/> entity and <see cref="CategoryDto"/> have equivalent values for all mapped properties.
    /// </summary>
    /// <param name="expected">The source <see cref="Category"/> entity.</param>
    /// <param name="actual">The <see cref="CategoryDto"/> to verify.</param>
    public static void AssertEquivalent(Category? expected, CategoryDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that a <see cref="CreateCategoryRequest"/> and <see cref="CreateCategoryResponse"/> share identical request and response values.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The response returned by the CreateAsync method.</param>
    public static void AssertEquivalent(CreateCategoryRequest? expected, CreateCategoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that a <see cref="CreateCategoryRequest"/> has been correctly mapped to a <see cref="Category"/> entity.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The entity created by the service.</param>
    public static void AssertEquivalent(CreateCategoryRequest? expected, Category? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that a <see cref="Category"/> entity matches the values returned in a <see cref="CreateCategoryResponse"/>, including the assigned Id.
    /// </summary>
    /// <param name="expected">The saved <see cref="Category"/> entity.</param>
    /// <param name="actual">The response DTO from CreateAsync.</param>
    public static void AssertEquivalent(Category? expected, CreateCategoryResponse actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateCategoryRequest"/> and <see cref="UpdateCategoryResponse"/> share identical update values.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The response returned by the UpdateAsync method.</param>
    public static void AssertEquivalent(UpdateCategoryRequest? expected, UpdateCategoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateCategoryRequest"/> has been applied correctly to a <see cref="Category"/> entity.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The entity after ApplyUpdate.</param>
    public static void AssertEquivalent(UpdateCategoryRequest? expected, Category? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    /// <summary>
    /// Asserts that a <see cref="Category"/> entity matches the values returned in an <see cref="UpdateCategoryResponse"/>, including the assigned Id.
    /// </summary>
    /// <param name="expected">The updated <see cref="Category"/> entity.</param>
    /// <param name="actual">The response DTO from UpdateAsync.</param>
    public static void AssertEquivalent(Category? expected, UpdateCategoryResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }
}
