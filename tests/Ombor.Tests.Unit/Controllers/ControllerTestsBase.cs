using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ombor.Tests.Unit.Controllers;

public abstract class ControllerTestsBase : UnitTestsBase
{
    protected const string PaginationHeader = "X-Pagination";
    protected readonly ControllerContext _controllerContext;
    protected readonly HttpResponse _response;

    protected ControllerTestsBase()
    {
        _controllerContext = GetControllerContext();
        _response = _controllerContext.HttpContext.Response;
    }

    private static ControllerContext GetControllerContext()
    {
        var httpContext = new DefaultHttpContext();
        return new ControllerContext { HttpContext = httpContext };
    }
}