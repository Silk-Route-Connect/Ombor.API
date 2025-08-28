using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Auth;

namespace Ombor.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        await service.RegisterAsync(request);

        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var response = await service.LoginAsync(request);

        return Ok();
    }
}
