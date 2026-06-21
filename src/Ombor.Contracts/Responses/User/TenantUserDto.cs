namespace Ombor.Contracts.Responses.User;

/// <summary>A user in the organization's Settings list (includes deactivated users).</summary>
/// <param name="Id">The user id.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Contact">The user's primary contact (phone).</param>
/// <param name="ContactType">The contact kind ("phone" or "email").</param>
/// <param name="Active">Whether the user can authenticate (rule 41 — deactivated users are kept, not deleted).</param>
/// <param name="Self">Whether this row is the current user.</param>
/// <param name="Online">Presence flag (always false — no presence tracking in v1).</param>
/// <param name="LastActiveAt">The deactivation date when inactive; null while active.</param>
public sealed record TenantUserDto(
    int Id,
    string Name,
    string Contact,
    string ContactType,
    bool Active,
    bool Self,
    bool Online,
    DateTimeOffset? LastActiveAt);
