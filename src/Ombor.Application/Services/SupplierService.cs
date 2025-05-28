using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class SupplierService(IApplicationDbContext context, IRequestValidator validator) : ISupplierService
{
    public Task<SupplierDto[]> GetAsync(GetSuppliersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) ||
             (x.Address != null && x.Address.Contains(request.SearchTerm)) ||
             (x.Email != null && x.Email.Contains(request.SearchTerm)) ||
             (x.CompanyName != null && x.CompanyName.Contains(request.SearchTerm)));
        }

        return query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto(
                x.Id, x.Name, x.Address,
                x.Email, x.CompanyName,
                x.IsActive, x.PhoneNumbers))
            .ToArrayAsync();
    }

    public async Task<SupplierDto> GetByIdAsync(GetSupplierByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<CreateSupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        context.Suppliers.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdateSupplierResponse> UpdateAsync(UpdateSupplierRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteSupplierRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        context.Suppliers.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Supplier> GetOrThrowAsync(int id) =>
        await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Supplier>(id);
}
