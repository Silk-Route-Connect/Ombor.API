namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Request to retrieve a single category by its ID.
/// </summary>
/// <param name="Id">The identifier of the category to retrieve. Must be &gt; 0.</param>
public sealed record GetCategoryByIdRequest(int Id);
