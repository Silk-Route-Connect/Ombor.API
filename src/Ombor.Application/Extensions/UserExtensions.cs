using Ombor.Domain.Entities;

namespace Ombor.Application.Extensions;

internal static class UserExtensions
{
    /// <summary>The user's full display name, or null when there is no user (seed/system rows).</summary>
    public static string? DisplayName(this User? user) =>
        user is null ? null : $"{user.FirstName} {user.LastName}";
}
