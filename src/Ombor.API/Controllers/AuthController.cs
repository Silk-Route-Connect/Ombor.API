using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.API.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.RegisterAsync(request);

        return Ok(new { Token = result });
    }

    [HttpPost("sms/verification")]
    public async Task<ActionResult> SmsVerificationAsync([FromBody] SmsVerificationRequest request)
    {
        var success = await service.SmsVerificationAsync(request);

        if (!success)
        {
            return BadRequest("Invalid verification code.");
        }

        return Ok("Phone number verified successfully.");
    }

    [HttpPost("login")]
    public async Task<ActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var response = await service.LoginAsync(request);

        return Ok(response);
    }
}
