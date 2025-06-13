using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class PartnerMappings
{
    public static PartnerDto ToDto(this Partner partner) =>
        new(
            partner.Id,
            partner.Name,
            partner.Address,
            partner.Email,
            partner.CompanyName,
            partner.IsActive,
            partner.Balance,
            partner.PhoneNumbers);

    public static Partner ToEntity(this CreatePartnerRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Email = request.Email,
            CompanyName = request.CompanyName,
            IsActive = request.IsActive,
            Balance = request.Balance,
            PhoneNumbers = request.PhoneNumbers
        };

    public static CreatePartnerResponse ToCreateResponse(this Partner partner) =>
        new(
            partner.Id,
            partner.Name,
            partner.Address,
            partner.Email,
            partner.CompanyName,
            partner.IsActive,
            partner.Balance,
            partner.PhoneNumbers);

    public static UpdatePartnerResponse ToUpdateResponse(this Partner partner) =>
        new(
            partner.Id,
            partner.Name,
            partner.Address,
            partner.Email,
            partner.CompanyName,
            partner.IsActive,
            partner.Balance,
            partner.PhoneNumbers);

    public static void ApplyUpdate(this Partner partner, UpdatePartnerRequest request)
    {
        partner.Name = request.Name;
        partner.Address = request.Address;
        partner.Email = request.Email;
        partner.CompanyName = request.CompanyName;
        partner.IsActive = request.IsActive;
        partner.Balance = request.Balance;
        partner.PhoneNumbers = request.PhoneNumbers;
    }
}
