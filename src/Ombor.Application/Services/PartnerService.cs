using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PartnerService(IApplicationDbContext context, IRequestValidator validator) : IPartnerService
{
    public Task<PartnerDto[]> GetAsync(GetpartnersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Partners.AsQueryable();

        var searchTerm = request.SearchTerm;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Address != null && x.Address.Contains(searchTerm)) ||
                (x.Email != null && x.Email.Contains(searchTerm)) ||
                (x.CompanyName != null && x.CompanyName.Contains(searchTerm)));
        }

        return query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new PartnerDto(
                x.Id,
                x.Name,
                x.Address,
                x.Email,
                x.CompanyName,
                x.IsActive,
                x.Balance,
                x.PhoneNumbers))
            .ToArrayAsync();
    }

    public async Task<PartnerDto> GetByIdAsync(GetPartnerByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<CreatePartnerResponse> CreateAsync(CreatePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        context.Partners.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdatePartnerResponse> UpdateAsync(UpdatePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeletePartnerRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        context.Partners.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Partner> GetOrThrowAsync(int id) =>
        await context.Partners.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Partner>(id);
}
