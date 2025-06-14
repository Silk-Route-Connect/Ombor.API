using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Tests.Common.Factories;

public static class PartnerRequestFactory
{
    public static CreatePartnerRequest GenerateValidCreateRequest()
        => new(Name: "Test Partner Name",
            Address: "Test Partner's address",
            Email: "test@gmail.com",
            CompanyName: "Partner OOO Limited",
            Balance: 10_000,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["+998-90-123-45-67", "+998911101212"]);

    public static CreatePartnerRequest GenerateInvalidCreateRequest()
        => new(Name: "", // Invalid name
            Address: "Some address",
            Email: "partner's email", // Invalid email
            CompanyName: "partner's company name",
            Balance: 1000.00m,
            Type: PartnerType.Customer,
            PhoneNumbers: ["asdasd", "++654++321"]); // Invalid phone numbers

    public static UpdatePartnerRequest GenerateValidUpdateRequest(int partnerId)
        => new(Id: partnerId,
            Name: "Updated partner Name",
            Address: "Updated Address",
            Email: "updated-email@gmail.com",
            CompanyName: "Updated Company name",
            Balance: 2000.00m,
            Type: PartnerType.Customer,
            PhoneNumbers: ["+998-90-123-45-67", "+998911101212"]);

    public static UpdatePartnerRequest GenerateInvalidUpdateRequest(int partnerId)
        => new(Id: partnerId,
            Name: "", // Invalid name
            Address: "Test Partner Address",
            Email: "updated-mail.com", // Invalid request
            CompanyName: "Updated Company name",
            Balance: 2000.00m,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["++//**--", "qwerty123"]); // Invalid phone numbers
}
