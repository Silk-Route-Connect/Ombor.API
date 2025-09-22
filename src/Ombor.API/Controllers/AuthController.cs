using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.API.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SendOtpResponse>> RegisterAsync([FromBody] RegisterRequest request)
    {
        var result = await service.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerificationResponse>> SmsVerificationAsync([FromBody] SmsVerificationRequest request)
    {
        var result = await service.VerifyRegistrationOtpAsync(request);

        if (!result.Success)
        {
            return BadRequest("Invalid verification code.");
        }

        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request)
    {
        var response = await service.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoginResponse>> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
    {
        var result = await service.RefreshTokenAsync(request);

        return Ok(result);
    }
}
