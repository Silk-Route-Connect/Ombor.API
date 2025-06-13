using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public class PartnerTestsBase : ServiceTestsBase
{
    protected readonly int partnerId = 1_000;
    protected readonly Partner[] _defaultpartners;
    private protected readonly PartnerService _service;

    protected PartnerTestsBase()
    {
        _defaultpartners = GenerateRandomPartners();
        Setuppartners(_defaultpartners);

        _service = new PartnerService(_mockContext.Object, _mockValidator.Object);
    }

    protected Partner[] GenerateRandomPartners(int count = 5)
        => Enumerable.Range(1, count)
        .Select(i => CreatePartner(i))
        .ToArray();

    protected Partner CreatePartner(int? id = null)
        => _builder.partnerBuilder
        .WithId(id ?? partnerId)
        .BuildAndPopulate();
}
