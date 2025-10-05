using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;

namespace Ombor.API.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(EmployeeDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto[]>> GetAsync(
        [FromQuery] GetEmployeesRequest request)
    {
        var response = await service.GetAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetEmployeeByIdAsync(
        [FromRoute] GetEmployeeByIdRequest request)
    {
        var response = await service.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateEmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEmployeeResponse>> PostAsync(
        [FromBody] CreateEmployeeRequest request)
    {
        var response = await service.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetEmployeeByIdAsync),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdateEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateEmployeeResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateEmployeeRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = $"Route ID {id} does not match body ID {request.Id}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await service.UpdateAsync(request);

        return Ok(response);
    }

    [HttpDelete("{Id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] DeleteEmployeeRequest request)
    {
        await service.DeleteAsync(request);

        return NoContent();
    }
}