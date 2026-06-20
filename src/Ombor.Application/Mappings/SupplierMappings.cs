using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class PartnerMappings
{
    public static Partner ToEntity(this CreatePartnerRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Email = request.Email,
            CompanyName = request.CompanyName,
            Type = Enum.Parse<PartnerType>(request.Type.ToString()),
            OpeningBalance = request.OpeningBalance,
            PhoneNumbers = request.PhoneNumbers
        };

    public static CreatePartnerResponse ToCreateResponse(this Partner partner) =>
        new(Id: partner.Id,
            Name: partner.Name,
            partner.Type.ToString(),
            Address: partner.Address,
            Email: partner.Email,
            CompanyName: partner.CompanyName,
            OpeningBalance: partner.OpeningBalance,
            OpeningDate: partner.OpeningDate,
            IsArchived: partner.IsArchived,
            PhoneNumbers: partner.PhoneNumbers);

    public static UpdatePartnerResponse ToUpdateResponse(this Partner partner) =>
        new(Id: partner.Id,
            Name: partner.Name,
            Type: partner.Type.ToString(),
            Address: partner.Address,
            Email: partner.Email,
            CompanyName: partner.CompanyName,
            OpeningBalance: partner.OpeningBalance,
            OpeningDate: partner.OpeningDate,
            IsArchived: partner.IsArchived,
            PhoneNumbers: partner.PhoneNumbers);

    // Opening balance/date are immutable (set once at creation) — the update never touches them.
    public static void ApplyUpdate(this Partner partner, UpdatePartnerRequest request)
    {
        partner.Name = request.Name;
        partner.Address = request.Address;
        partner.Email = request.Email;
        partner.CompanyName = request.CompanyName;
        partner.PhoneNumbers = request.PhoneNumbers;
        partner.Type = Enum.Parse<PartnerType>(request.Type.ToString());
    }
}
