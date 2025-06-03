
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public abstract class SupplierTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "John";

    protected override string GetUrl()
        => Routes.Supplier;

    protected override string GetUrl(int id)
    => $"{Routes.Supplier}/{id}";

    protected async Task<int> CreateSupplierAsync()
    {
        var supplier = new Supplier
        {
            Name = "John",
            Address = "Tashkent",
            Email = "johnjohn123@gmail.com",
            CompanyName = "Company Name LLC",
            IsActive = true,
            PhoneNumbers = ["+998974561100"]
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return supplier.Id;
    }

    protected async Task<int> CreateSupplierAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return supplier.Id;
    }
}
