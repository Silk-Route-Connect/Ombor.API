using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal sealed class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
