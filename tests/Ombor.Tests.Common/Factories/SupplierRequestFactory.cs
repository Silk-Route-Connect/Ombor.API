using Ombor.Contracts.Requests.Supplier;

namespace Ombor.Tests.Common.Factories;

public static class SupplierRequestFactory
{
    private const int DefaultSupplierId = 123;

    public static CreateSupplierRequest GenerateValidCreateRequest()
        => new(
            Name: "Test Supplier Name",
            Address: "Test Supplier's address",
            Email: "Supplier's email",
            CompanyName: "Supplier's company name",
            IsActive: true,
            Balance: 1000.00m,
            PhoneNumbers: ["+998913456789", "+998994441122"]
        );

    public static CreateSupplierRequest GenerateInvalidCreateRequest()
        => new(
            Name: "",
            Address: "",
            Email: "Supplier's email",
            CompanyName: "Supplier's company name",
            IsActive: true,
            Balance: 1000.00m,
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
            Balance: 2000.00m,
            PhoneNumbers: ["+998913456789", "+998931233211"]
        );

    public static UpdateSupplierRequest GenerateInvalidUpdateRequest(int? supplierId)
        => new(
            Id: supplierId ?? DefaultSupplierId,
            Name: "",
            Address: "asdasfasdfcxzasdaszxcasdasdxasxasdas",
            Email: "Updated Supplier's email",
            CompanyName: "Updated Supplier's company name",
            IsActive: true,
            Balance: 2000.00m,
            PhoneNumbers: ["++//**--", "qwerty123"]
        );
}
