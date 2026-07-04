using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;

namespace Ombor.Application.Interfaces;

public interface ITemplateService
{
    Task<TemplateDto[]> GetAsync(GetTemplatesRequest request);
    Task<TemplateDto> GetByIdAsync(GetTemplateByIdRequest request);

    /// <summary>Stamps the template's <c>LastUsedAt</c> to now — called when it is loaded into a new transaction.</summary>
    Task<TemplateDto> MarkUsedAsync(int id);

    Task<CreateTemplateResponse> CreateAsync(CreateTemplateRequest request);
    Task<UpdateTemplateResponse> UpdateAsync(UpdateTemplateRequest request);
    Task DeleteAsync(DeleteTemplateRequest request);
}
