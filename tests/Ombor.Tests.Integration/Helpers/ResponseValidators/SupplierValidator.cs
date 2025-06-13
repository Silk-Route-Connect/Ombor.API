using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class SupplierValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetSuppliersRequest request, SupplierDto[] response)
    {
        var exceptedSuppliers = await GetSuppliersAsync(request);

        Assert.Equal(exceptedSuppliers.Length, response.Length);
        Assert.All(exceptedSuppliers, expected =>
        {
            var actual = response.FirstOrDefault(s => s.Id == expected.Id);

            SupplierAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int supplierId, SupplierDto response)
    {
        var expected = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        SupplierAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateSupplierRequest request, CreateSupplierResponse response)
    {
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == response.Id);

        SupplierAssertionHelper.AssertEquivalent(request, response);
        SupplierAssertionHelper.AssertEquivalent(request, supplier);
        SupplierAssertionHelper.AssertEquivalent(supplier, response);
    }

    public async Task ValidatePutAsync(UpdateSupplierRequest request, UpdateSupplierResponse response)
    {
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id);

        SupplierAssertionHelper.AssertEquivalent(request, response);
        SupplierAssertionHelper.AssertEquivalent(request, supplier);
        SupplierAssertionHelper.AssertEquivalent(supplier, response);
    }

    public async Task ValidateDeleteAsync(int supplierId)
    {
        var supplier = await context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        Assert.Null(supplier);
    }

    private async Task<Partner[]> GetSuppliersAsync(GetSuppliersRequest request)
    {
        var query = context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(s => s.Name.Contains(request.SearchTerm) ||
            (s.Address != null && s.Address.Contains(request.SearchTerm)) ||
            (s.Email != null && s.Email.Contains(request.SearchTerm)) ||
            (s.CompanyName != null && s.CompanyName.Contains(request.SearchTerm)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToArrayAsync();
    }
}
