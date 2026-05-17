namespace Ombor.Application.Interfaces;

/// <summary>
/// Resolves the user behind the current request. Returns <c>null</c> outside an
/// authenticated request (seeding, background work, design-time tooling).
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserId { get; }
}
