using Ombor.Contracts.Requests.Supplier;

namespace Ombor.Tests.Common.Factories;

public static class SupplierRequestFactory
{
    private const int DefaultSupplierId = 10;

    public static CreateSupplierRequest GenerateValidCreateRequest()
    => new(
        Name: "Test Supplier Name",
        Address: "Test Supplier's address",
        Email: "Supplier's email",
        CompanyName: "Supplier's company name",
        IsActive: true,
        PhoneNumbers: ["+998913456789", "+998994441122"]
    );

    public static CreateSupplierRequest GenerateInvalidCreateRequest()
    => new(
        Name: "",
        Address: "",
        Email: "Supplier's email",
        CompanyName: "Supplier's company name",
        IsActive: true,
        PhoneNumbers: ["asdasd", "++654++321"]
    );

    public static UpdateSupplierRequest GenerateValidUpdateRequest(int? supplierId)
    => new(
        Id: supplierId ?? DefaultSupplierId,
        Name: "Updated Supplier Name",
        Address: "Updated Supplier's address",
        Email: "Updated Supplier's email",
        CompanyName: "Updated Supplier's company name",
        IsActive: false,
        PhoneNumbers: ["+998913456789", "+998931233211"]
    );

    public static UpdateSupplierRequest GenerateInalidUpdateRequest(int? supplierId)
    => new(
        Id: supplierId ?? DefaultSupplierId,
        Name: "",
        Address: "asdasfasdfcxzasdaszxcasdasdxasxasdas",
        Email: "Updated Supplier's email",
        CompanyName: "Updated Supplier's company name",
        IsActive: true,
        PhoneNumbers: ["++//**--", "qwerty123"]
    );
}
