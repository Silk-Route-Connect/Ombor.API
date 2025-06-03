using Ombor.Contracts.Requests.Supplier;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class SupplierExtensions
{
    public static bool IsEquivalent(this Supplier supplier, CreateSupplierRequest request) =>
        supplier.Name == request.Name &&
        supplier.Address == request.Address &&
        supplier.Email == request.Email &&
        supplier.CompanyName == request.CompanyName &&
        supplier.IsActive == request.IsActive &&
        supplier.PhoneNumbers == request.PhoneNumbers;
}
