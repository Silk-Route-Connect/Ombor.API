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
        var result = await service.RegisterAsync(request);

        return Accepted(result.Message);
    }

    [HttpPost("sms/verification")]
    public async Task<ActionResult> SmsVerificationAsync([FromBody] SmsVerificationRequest request)
    {
        var result = await service.VerifyRegistrationOtpAsync(request);

        if (!result.Success)
        {
            return BadRequest("Invalid verification code.");
        }

        return Ok("Phone number successfully verified .");
    }

    [HttpPost("login")]
    public async Task<ActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var response = await service.LoginAsync(request);

        return Ok(response);
    }
}
