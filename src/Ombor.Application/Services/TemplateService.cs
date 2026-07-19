using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TemplateService(IApplicationDbContext context, IRequestValidator validator) : ITemplateService
{
    public async Task<TemplateDto[]> GetAsync(GetTemplatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        var templates = await query
            .OrderBy(x => x.Name)
            .ToArrayAsync();

        return [.. templates.Select(x => x.ToDto())];
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

        // Package-entry items are resolved to base units server-side from the product's package size (rule 21).
        var packageSizes = await context.LoadPackageSizesAsync(
            request.Items.Where(i => i.PackageQuantity is > 0).Select(i => i.ProductId));

        var entity = request.ToEntity(packageSizes);

        context.Templates.Add(entity);
        await context.SaveChangesAsync();

        var createdTemplate = await GetOrThrowAsync(entity.Id);

        return createdTemplate.ToCreateResponse();
    }

    public async Task<UpdateTemplateResponse> UpdateAsync(UpdateTemplateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // Items must be tracked so the update can reconcile them (update existing, add new, remove dropped).
        var template = await context.Templates
            .Include(x => x.Partner)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw new EntityNotFoundException<Template>(request.Id);

        // Package-entry items are resolved to base units server-side from the product's package size (rule 21).
        var packageSizes = await context.LoadPackageSizesAsync(
            request.Items.Where(i => i.PackageQuantity is > 0).Select(i => i.ProductId));

        template.ApplyUpdate(request, packageSizes);

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

    private async Task<Template> GetOrThrowAsync(int id)
        => await context.Templates
        .Include(x => x.Partner)
        .FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Template>(id);
}
