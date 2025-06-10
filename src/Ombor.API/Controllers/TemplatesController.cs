using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;

namespace Ombor.API.Controllers;

[Route("api/templates")]
[ApiController]
public class TemplatesController(ITemplateService templateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TemplateDto[]>> GetAsync(
        [FromQuery] GetTemplatesRequest request)
    {
        var response = await templateService.GetAsync(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TemplateDto>> GetTemplateByIdAsync(
        [FromRoute] GetTemplateByIdRequest request)
    {
        var response = await templateService.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CreateTemplateResponse>> PostAsync(
        [FromBody] CreateTemplateRequest request)
    {
        var response = await templateService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetTemplateByIdAsync),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UpdateTemplateResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateTemplateRequest request)
    {
        var response = await templateService.UpdateAsync(request);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] DeleteTemplateRequest request)
    {
        await templateService.DeleteAsync(request);

        return NoContent();
    }
}
