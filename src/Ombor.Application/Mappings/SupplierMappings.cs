using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class SupplierMappings
{
    public static SupplierDto ToDto(this Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.Address,
            supplier.Email,
            supplier.CompanyName,
            supplier.IsActive,
            supplier.Balance,
            supplier.PhoneNumbers);

    public static Supplier ToEntity(this CreateSupplierRequest request) =>
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

    public static CreateSupplierResponse ToCreateResponse(this Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.Address,
            supplier.Email,
            supplier.CompanyName,
            supplier.IsActive,
            supplier.Balance,
            supplier.PhoneNumbers);

    public static UpdateSupplierResponse ToUpdateResponse(this Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.Address,
            supplier.Email,
            supplier.CompanyName,
            supplier.IsActive,
            supplier.Balance,
            supplier.PhoneNumbers);

    public static void ApplyUpdate(this Supplier supplier, UpdateSupplierRequest request)
    {
        supplier.Name = request.Name;
        supplier.Address = request.Address;
        supplier.Email = request.Email;
        supplier.CompanyName = request.CompanyName;
        supplier.IsActive = request.IsActive;
        supplier.Balance = request.Balance;
        supplier.PhoneNumbers = request.PhoneNumbers;
    }
}
