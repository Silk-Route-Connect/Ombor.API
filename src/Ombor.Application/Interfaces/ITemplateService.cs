using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;

namespace Ombor.Application.Interfaces;

public interface ITemplateService
{
    Task<PagedList<TemplateDto>> GetAsync(GetTemplatesRequest request);
    Task<TemplateDto> GetByIdAsync(GetTemplateByIdRequest request);
    Task<CreateTemplateResponse> CreateAsync(CreateTemplateRequest request);
    Task<UpdateTemplateResponse> UpdateAsync(UpdateTemplateRequest request);
    Task DeleteAsync(DeleteTemplateRequest request);
}
