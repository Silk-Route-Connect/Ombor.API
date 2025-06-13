using Ombor.Contracts.Requests.Partner;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class PartnerExtension
{
    public static bool IsEquivalent(this Partner partner, CreatePartnerRequest request) =>
        partner.Name == request.Name &&
        partner.Address == request.Address &&
        partner.Email == request.Email &&
        partner.CompanyName == request.CompanyName &&
        partner.IsActive == request.IsActive &&
        partner.PhoneNumbers == request.PhoneNumbers;
}
