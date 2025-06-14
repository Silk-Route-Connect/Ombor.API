using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class PartnerValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetPartnersRequest request, PartnerDto[] response)
    {
        var exceptedpartners = await GetAsync(request);

        Assert.Equal(exceptedpartners.Length, response.Length);
        Assert.All(exceptedpartners, expected =>
        {
            var actual = response.FirstOrDefault(s => s.Id == expected.Id);

            PartnerAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int partnerId, PartnerDto response)
    {
        var expected = await context.Partners
            .FirstOrDefaultAsync(s => s.Id == partnerId);

        PartnerAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreatePartnerRequest request, CreatePartnerResponse response)
    {
        var partner = await context.Partners
            .FirstOrDefaultAsync(s => s.Id == response.Id);

        PartnerAssertionHelper.AssertEquivalent(request, response);
        PartnerAssertionHelper.AssertEquivalent(request, partner);
        PartnerAssertionHelper.AssertEquivalent(partner, response);
    }

    public async Task ValidatePutAsync(UpdatePartnerRequest request, UpdatePartnerResponse response)
    {
        var partner = await context.Partners
            .FirstOrDefaultAsync(s => s.Id == request.Id);

        PartnerAssertionHelper.AssertEquivalent(request, response);
        PartnerAssertionHelper.AssertEquivalent(request, partner);
        PartnerAssertionHelper.AssertEquivalent(partner, response);
    }

    public async Task ValidateDeleteAsync(int partnerId)
    {
        var partner = await context.Partners
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == partnerId);

        Assert.Null(partner);
    }

    private async Task<Partner[]> GetAsync(GetPartnersRequest request)
    {
        var query = context.Partners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(
                s => s.Name.Contains(request.SearchTerm) ||
                (s.Address != null && s.Address.Contains(request.SearchTerm)) ||
                (s.Email != null && s.Email.Contains(request.SearchTerm)) ||
                (s.CompanyName != null && s.CompanyName.Contains(request.SearchTerm)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToArrayAsync();
    }
}
