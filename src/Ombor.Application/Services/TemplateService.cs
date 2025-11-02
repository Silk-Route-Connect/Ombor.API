using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TemplateService(
    IApplicationDbContext context,
    IRequestValidator validator) : ITemplateService
{
    public async Task<PagedList<TemplateDto>> GetAsync(GetTemplatesRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var templates = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = templates.Select(x => x.ToDto());

        return PagedList<TemplateDto>.ToPagedList(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<TemplateDto> GetByIdAsync(GetTemplateByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var template = await GetOrThrowAsync(request.Id);

        return template.ToDto();
    }

    public async Task<CreateTemplateResponse> CreateAsync(CreateTemplateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();

        context.Templates.Add(entity);
        await context.SaveChangesAsync();

        var createdTemplate = await GetOrThrowAsync(entity.Id);

        return createdTemplate.ToCreateResponse();
    }

    public async Task<UpdateTemplateResponse> UpdateAsync(UpdateTemplateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var template = await GetOrThrowAsync(request.Id);
        template.ApplyUpdate(request);

        await context.SaveChangesAsync();

        var updatedTemplate = await GetOrThrowAsync(request.Id);

        return updatedTemplate.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteTemplateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var template = await GetOrThrowAsync(request.Id);

        context.Templates.Remove(template);
        await context.SaveChangesAsync();
    }

    private IQueryable<Template> GetQuery(GetTemplatesRequest request)
    {
        var query = context.Templates
            .Include(x => x.Partner)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm));
        }

        if (request.Type.HasValue)
        {
            var domainType = request.Type.Value.ToDomain();
            query = query.Where(x => x.Type == domainType);
        }

        return query;
    }

    private IQueryable<Template> ApplySort(IQueryable<Template> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "name_desc" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

    private async Task<Template> GetOrThrowAsync(int id)
        => await context.Templates
        .Include(x => x.Partner)
        .FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Template>(id);
}
