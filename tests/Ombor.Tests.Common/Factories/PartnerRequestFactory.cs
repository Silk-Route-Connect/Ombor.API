using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Tests.Common.Factories;

public static class PartnerRequestFactory
{
    private const int DefaultPartnerId = 123;

    public static CreatePartnerRequest GenerateValidCreateRequest()
        => new(Name: "Test partner Name",
            Address: "Test partner's address",
            Email: "partner's email",
            CompanyName: "partner's company name",
            Balance: 1000.00m,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["+998913456789", "+998994441122"]);

    public static CreatePartnerRequest GenerateInvalidCreateRequest()
        => new(Name: "",
            Address: "",
            Email: "partner's email",
            CompanyName: "partner's company name",
            Balance: 1000.00m,
            Type: PartnerType.Customer,
            PhoneNumbers: ["asdasd", "++654++321"]);

    public static UpdatePartnerRequest GenerateValidUpdateRequest(int? partnerId)
        => new(Id: partnerId ?? DefaultPartnerId,
            Name: "Updated partner Name",
            Address: "Updated partner's address",
            Email: "Updated partner's email",
            CompanyName: "Updated partner's company name",
            Balance: 2000.00m,
            Type: PartnerType.Customer,
            PhoneNumbers: ["+998913456789", "+998931233211"]);

    public static UpdatePartnerRequest GenerateInvalidUpdateRequest(int? partnerId)
        => new(Id: partnerId ?? DefaultPartnerId,
            Name: "",
            Address: "Test Partner Address",
            Email: "Updated partner's email",
            CompanyName: "Updated partner's company name",
            Balance: 2000.00m,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["++//**--", "qwerty123"]);
}
