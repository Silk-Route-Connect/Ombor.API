using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class PartnerMappings
{
    public static PartnerDto ToDto(this Partner partner) =>
        new(Id: partner.Id,
            Name: partner.Name,
            Type: partner.Type.ToString(),
            Address: partner.Address,
            Email: partner.Email,
            CompanyName: partner.CompanyName,
            Balance: partner.Balance,
            PhoneNumbers: partner.PhoneNumbers);

    public static Partner ToEntity(this CreatePartnerRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Email = request.Email,
            CompanyName = request.CompanyName,
            Type = Enum.Parse<PartnerType>(request.Type.ToString()),
            Balance = request.Balance,
            PhoneNumbers = request.PhoneNumbers
        };

    public static CreatePartnerResponse ToCreateResponse(this Partner partner) =>
        new(Id: partner.Id,
            Name: partner.Name,
            partner.Type.ToString(),
            Address: partner.Address,
            Email: partner.Email,
            CompanyName: partner.CompanyName,
            Balance: partner.Balance,
            PhoneNumbers: partner.PhoneNumbers);

    public static UpdatePartnerResponse ToUpdateResponse(this Partner partner) =>
        new(Id: partner.Id,
            Name: partner.Name,
            Type: partner.Type.ToString(),
            Address: partner.Address,
            Email: partner.Email,
            CompanyName: partner.CompanyName,
            Balance: partner.Balance,
            PhoneNumbers: partner.PhoneNumbers);

    public static void ApplyUpdate(this Partner partner, UpdatePartnerRequest request)
    {
        partner.Name = request.Name;
        partner.Address = request.Address;
        partner.Email = request.Email;
        partner.CompanyName = request.CompanyName;
        partner.Balance = request.Balance;
        partner.PhoneNumbers = request.PhoneNumbers;
        partner.Type = Enum.Parse<PartnerType>(request.Type.ToString());
    }
}
