using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit;

namespace Ombor.Tests.Integration.Endpoints.OrganizationScoping;

/// <summary>
/// Proves the <c>View_PartnerBalance</c> projection is isolated per organization by the
/// global query filter — closing the scoping hole the M0 audit found (it was the one
/// organization-owned projection that was not filtered).
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class PartnerBalanceScopingTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory _factory;

    public PartnerBalanceScopingTests(TestingWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task PartnerBalance_IsScopedToOwningOrganization()
    {
        // Arrange — one partner in organization 1, one in organization 2.
        await using var org1Context = CreateContext(organizationId: 1);
        await using var org2Context = CreateContext(organizationId: 2);

        var org1PartnerId = await AddPartnerAsync(org1Context, "Org1 Partner");
        var org2PartnerId = await AddPartnerAsync(org2Context, "Org2 Partner");

        // Act — read the balance view through each organization's filtered context.
        var org1Balances = await org1Context.PartnerBalances.ToListAsync();
        var org2Balances = await org2Context.PartnerBalances.ToListAsync();

        // Assert — each context sees only its own partner's balance row, never the other's.
        Assert.Contains(org1Balances, x => x.PartnerId == org1PartnerId);
        Assert.DoesNotContain(org1Balances, x => x.PartnerId == org2PartnerId);

        Assert.Contains(org2Balances, x => x.PartnerId == org2PartnerId);
        Assert.DoesNotContain(org2Balances, x => x.PartnerId == org1PartnerId);
    }

    private static async Task<int> AddPartnerAsync(ApplicationDbContext context, string name)
    {
        var partner = new Partner
        {
            Name = $"{name} {Guid.NewGuid():N}",
            Type = PartnerType.Both,
            PhoneNumbers = ["+998900000000"],
        };

        context.Partners.Add(partner);
        await context.SaveChangesAsync();

        return partner.Id;
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
