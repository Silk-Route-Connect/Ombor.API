using Microsoft.AspNetCore.Mvc;

namespace Ombor.Tests.Integration.Extensions;

public static class AssertExtensions
{
    public static void ShouldBeNotFound<TEntity>(this ProblemDetails problemDetails, int id)
    {
        var typeName = typeof(TEntity).Name;

        Assert.Equal("Not Found", problemDetails.Title);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal(GetNotFoundErrorMessage(id, typeName), problemDetails.Detail);
    }

    private static string GetNotFoundErrorMessage(int id, string typeName)
    {
        return $"{typeName} with ID {id} was not found.";
    }
}
