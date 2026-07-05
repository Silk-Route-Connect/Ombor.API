using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit;

namespace Ombor.Tests.Integration.Endpoints.OrganizationScoping;

/// <summary>
/// Proves a payment and its components and allocations are isolated per organization by the global query
/// filter (the money ledger must never leak across tenants — hard-rule #5 / business rule 34).
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class PaymentScopingTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory _factory;

    public PaymentScopingTests(TestingWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Payment_ComponentsAndAllocations_AreVisibleOnlyToOwningOrganization()
    {
        // Arrange — a full payment (source component + allocation) in organization 1, another in organization 2.
        await using var org1Context = CreateContext(organizationId: 1);
        await using var org2Context = CreateContext(organizationId: 2);

        var (p1, c1, a1) = await AddPaymentAsync(org1Context);
        var (p2, c2, a2) = await AddPaymentAsync(org2Context);

        // Act
        var org1Payments = await org1Context.Payments.Select(x => x.Id).ToListAsync();
        var org2Payments = await org2Context.Payments.Select(x => x.Id).ToListAsync();
        var org1Components = await org1Context.Set<PaymentComponent>().Select(x => x.Id).ToListAsync();
        var org2Components = await org2Context.Set<PaymentComponent>().Select(x => x.Id).ToListAsync();
        var org1Allocations = await org1Context.Set<PaymentAllocation>().Select(x => x.Id).ToListAsync();
        var org2Allocations = await org2Context.Set<PaymentAllocation>().Select(x => x.Id).ToListAsync();

        // Assert — each organization sees only its own payment graph; the other's is filtered out.
        Assert.Contains(p1, org1Payments);
        Assert.DoesNotContain(p2, org1Payments);
        Assert.Contains(p2, org2Payments);
        Assert.DoesNotContain(p1, org2Payments);

        Assert.Contains(c1, org1Components);
        Assert.DoesNotContain(c2, org1Components);
        Assert.DoesNotContain(c1, org2Components);

        Assert.Contains(a1, org1Allocations);
        Assert.DoesNotContain(a2, org1Allocations);
        Assert.DoesNotContain(a1, org2Allocations);
    }

    private static async Task<(int paymentId, int componentId, int allocationId)> AddPaymentAsync(ApplicationDbContext context)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = PartnerType.Both };
        context.Wallets.Add(wallet);
        context.Partners.Add(partner);
        await context.SaveChangesAsync();

        var payment = new Payment
        {
            Number = $"PAY-{Guid.NewGuid():N}",
            Type = PaymentType.General,
            Direction = PaymentDirection.Income,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = partner.Id,
            WalletId = wallet.Id,
        };
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = 1_000m,
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            Type = PaymentAllocationType.AdvanceCredit,
            Amount = 1_000m,
        });
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return (payment.Id, payment.Components.First().Id, payment.Allocations.First().Id);
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
