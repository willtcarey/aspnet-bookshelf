using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookshelf.Tests.TestSupport;

internal static class ControllerTestContext
{
    public static void AttachHttpContext(Controller controller, HttpContext? httpContext = null)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext ?? new DefaultHttpContext()
        };
    }
}
