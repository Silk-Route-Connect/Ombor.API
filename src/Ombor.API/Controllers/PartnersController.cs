using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Partner;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage partners.
/// </summary>
[ApiController]
[Route("api/partners")]
public sealed class PartnersController(
    IPartnerService partnerService,
    IPaymentService paymentService) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of partners, with optional filtering by search term.
    /// </summary>
    /// <param name="request">Filtering and paging parameters.</param>
    /// <returns>Array of <see cref="PartnerDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PartnerDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<PartnerDto[]>> GetAsync(
        [FromQuery] GetPartnersRequest request)
    {
        var response = await partnerService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific partner by its unique ID.
    /// </summary>
    /// <param name="request">Request containing the partner ID.</param>
    /// <returns>The matching <see cref="PartnerDto"/>.</returns>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(PartnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartnerDto>> GetPartnerByIdAsync([FromRoute] GetPartnerByIdRequest request)
    {
        var response = await partnerService.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}/payments")]
    [ProducesResponseType(typeof(PaymentDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto[]>> GetPartnerPaymentsAsync([FromRoute] int id)
    {
        var request = new GetPaymentsRequest { PartnerId = id };
        var response = await paymentService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new partner.
    /// </summary>
    /// <param name="request">Payload describing the partner to create.</param>
    /// <returns>The created partner details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePartnerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePartnerResponse>> PostAsync(
        [FromBody] CreatePartnerRequest request)
    {
        var response = await partnerService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetPartnerByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Updates an existing partner.
    /// </summary>
    /// <param name="id">The ID of the partner to update.</param>
    /// <param name="request">Payload describing the partner to update(must include the ID of the partner).</param>
    /// <returns>The updated partner details.</returns>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdatePartnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdatePartnerResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdatePartnerRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Id mismatch",
                Detail = $"Route ID ({id}) does not match body ID ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await partnerService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a partner by its ID.
    /// </summary>
    /// <param name="request">Delete request containing the partner ID.</param>
    /// <returns></returns>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(
            [FromRoute] DeletePartnerRequest request)
    {
        await partnerService.DeleteAsync(request);

        return NoContent();
    }
}