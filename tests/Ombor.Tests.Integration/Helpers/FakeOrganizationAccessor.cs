using Ombor.Application.Interfaces;

namespace Ombor.Tests.Integration.Helpers;

/// <summary>
/// Test <see cref="IOrganizationAccessor"/> with a fixed organization. Integration tests run
/// entirely within a single organization (id 1, matching <see cref="AuthHandler"/>).
/// </summary>
public sealed class FakeOrganizationAccessor(int? organizationId = 1) : IOrganizationAccessor
{
    public int? OrganizationId { get; private set; } = organizationId;

    public void SetOrganization(int organizationId) => OrganizationId = organizationId;
}
