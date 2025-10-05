using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PartnerService(IApplicationDbContext context, IRequestValidator validator) : IPartnerService
{
    public async Task<PagedList<PartnerDto>> GetAsync(GetPartnersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var partnersWithBalances = await query
            .Join(
                context.PartnerBalances,
                partner => partner.Id,
                balance => balance.PartnerId,
                (partner, balances) => new { partner, balances })
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dto = partnersWithBalances.Select(
               pb => new PartnerDto(
                   pb.partner.Id,
                   pb.partner.Name,
                   pb.partner.Type.ToString(),
                   pb.partner.Address,
                   pb.partner.Email,
                   pb.partner.CompanyName,
                   pb.balances.Total,
                   pb.partner.PhoneNumbers,
                   new PartnerBalanceDto(
                       pb.balances.Total,
                       pb.balances.PartnerAdvance,
                       pb.balances.CompanyAdvance,
                       pb.balances.PayableDebt,
                       pb.balances.ReceivableDebt)));

        return PagedList<PartnerDto>.ToPagedList(dto, totalCount, request.PageNumber, request.PageSize);
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

    private IQueryable<Partner> GetQuery(GetPartnersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Partners.AsNoTracking();

        var searchTerm = request.SearchTerm;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Address != null && x.Address.Contains(searchTerm)) ||
                (x.Email != null && x.Email.Contains(searchTerm)) ||
                (x.CompanyName != null && x.CompanyName.Contains(searchTerm)));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == Enum.Parse<PartnerType>(request.Type.Value.ToString()));
        }

        return query;
    }

    private IQueryable<Partner> ApplySort(IQueryable<Partner> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "email_asc" => query.OrderBy(x => x.Email),
            "email_desc" => query.OrderByDescending(x => x.Email),
            "address_asc" => query.OrderBy(x => x.Address),
            "address_desc" => query.OrderByDescending(x => x.Address),
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "companyName_asc" => query.OrderBy(x => x.CompanyName),
            "companyName_desc" => query.OrderByDescending(x => x.CompanyName),
            "balance_asc" => query.OrderBy(x => x.Balance),
            "balance_desc" => query.OrderByDescending(x => x.Balance),
            "name_desc" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name),
        };
}
