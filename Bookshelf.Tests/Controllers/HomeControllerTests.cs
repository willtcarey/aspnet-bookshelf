using System.Diagnostics;
using Bookshelf.Controllers;
using Bookshelf.Models;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bookshelf.Tests.Controllers;

public sealed class HomeControllerTests : IDisposable
{
    private readonly HomeController _controller =
        new(Mock.Of<ILogger<HomeController>>());

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void IndexReturnsView()
    {
        Assert.IsType<ViewResult>(_controller.Index());
    }

    [Fact]
    public void PrivacyReturnsView()
    {
        Assert.IsType<ViewResult>(_controller.Privacy());
    }

    [Fact]
    public void ErrorNoCurrentActivityUsesTraceIdentifierFromHttpContext()
    {
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-abc" };
        ControllerTestContext.AttachHttpContext(_controller, httpContext);

        var view = Assert.IsType<ViewResult>(_controller.Error());
        var model = Assert.IsType<ErrorViewModel>(view.Model);
        Assert.Equal("trace-abc", model.RequestId);
    }

    [Fact]
    public void ErrorWithCurrentActivityUsesActivityId()
    {
        ControllerTestContext.AttachHttpContext(_controller, new DefaultHttpContext { TraceIdentifier = "trace-fallback" });

        using var activity = new Activity("test-op");
        activity.Start();
        try
        {
            var view = Assert.IsType<ViewResult>(_controller.Error());
            var model = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.Equal(activity.Id, model.RequestId);
        }
        finally
        {
            activity.Stop();
        }
    }
}
