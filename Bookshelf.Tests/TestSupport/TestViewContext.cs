using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Bookshelf.Tests.TestSupport;

public static class TestViewContext
{
    public static ViewContext Create()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

        return new ViewContext(
            actionContext,
            new FakeView(),
            viewData,
            new TempDataDictionary(httpContext, new FakeTempDataProvider()),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    private sealed class FakeView : IView
    {
        public string Path => string.Empty;
        public Task RenderAsync(ViewContext context) => Task.CompletedTask;
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
