using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class TemplateValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetTemplatesRequest request, TemplateDto[] response)
    {
        var expectedTemplates = await GetTemplatesAsync(request);

        TemplateAssertionHelpers.AssertEquivalent(expectedTemplates, response);
    }

    public async Task ValidateGetByIdAsnc(int templateId, TemplateDto response)
    {
        var expectedTemplate = await context.Templates
            .FirstOrDefaultAsync(x => x.Id == templateId);

        Assert.NotNull(expectedTemplate);

        TemplateAssertionHelpers.AssertEquivalent(expectedTemplate, response);
    }

    public async Task ValidatePostAsync(CreateTemplateRequest request, CreateTemplateResponse response)
    {
        var createdTemplate = await context.Templates
            .FirstOrDefaultAsync(x => x.Id == response.Id);

        Assert.NotNull(createdTemplate);

        TemplateAssertionHelpers.AssertEquivalent(request, response);
        TemplateAssertionHelpers.AssertEquivalent(request, createdTemplate);
    }

    public async Task ValidatePutAsync(UpdateTemplateRequest request, UpdateTemplateResponse response)
    {
        var updatedTemplate = await context.Templates
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        Assert.NotNull(updatedTemplate);

        TemplateAssertionHelpers.AssertEquivalent(request, response);
        TemplateAssertionHelpers.AssertEquivalent(request, updatedTemplate);
    }

    public async Task ValidateDeleteAsync(int templateId)
    {
        var deletedTemplate = await context.Templates
            .FirstOrDefaultAsync(x => x.Id == templateId);

        Assert.Null(deletedTemplate);
    }

    private async Task<Template[]> GetTemplatesAsync(GetTemplatesRequest request)
    {
        var query = context.Templates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm));
        }

        if (request.Type.HasValue)
        {
            var domainType = Enum.Parse<Domain.Enums.TemplateType>(request.Type.Value.ToString());
            query = query.Where(x => x.Type == domainType);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToArrayAsync();
    }
}
