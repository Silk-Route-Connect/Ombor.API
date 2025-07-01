using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TransactionEndpoints;

public abstract class TransactionTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EndpointTestsBase(factory, outputHelper)
{
    protected override string GetUrl()
        => Routes.Transaction;

    protected override string GetUrl(int id)
        => $"{Routes.Transaction}/{id}";

    protected async Task<Partner> CreatePartnerAsync(Partner partner)
    {
        ArgumentNullException.ThrowIfNull(partner);

        try
        {
            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine(ex.Message);
        }

        return partner;
    }
}
