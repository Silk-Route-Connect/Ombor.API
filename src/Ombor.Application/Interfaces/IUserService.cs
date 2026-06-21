using Ombor.Contracts.Requests.User;
using Ombor.Contracts.Responses.User;

namespace Ombor.Application.Interfaces;

/// <summary>
/// User management for the current organization (Settings). Users are deactivated, never deleted
/// (rule 41), so they stay resolvable as audit actors.
/// </summary>
public interface IUserService
{
    /// <summary>All users in the current organization, including deactivated ones.</summary>
    Task<TenantUserDto[]> GetUsersAsync();

    /// <summary>Invites a user by phone, creating a usable account immediately.</summary>
    Task<TenantUserDto> InviteAsync(InviteUserRequest request);

    /// <summary>Deactivates a user (cannot be the current user).</summary>
    Task<TenantUserDto> DeactivateAsync(int userId);

    /// <summary>Reactivates a previously deactivated user.</summary>
    Task<TenantUserDto> ReactivateAsync(int userId);

    /// <summary>Sets the current user's interface language.</summary>
    Task SetLanguageAsync(SetLanguageRequest request);
}
