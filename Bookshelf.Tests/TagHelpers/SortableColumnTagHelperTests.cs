using Bookshelf.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Bookshelf.Tests.TagHelpers;

public class SortableColumnTagHelperTests
{
    [Fact]
    public void Process_RendersThElement()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("th", output.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    [Fact]
    public void Process_WhenColumnIsNotCurrentSort_BuildsAscLink()
    {
        var helper = BuildHelper("Title", currentSort: "Year", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("sort=Title", html);
        Assert.Contains("dir=asc", html);
    }

    [Fact]
    public void Process_WhenColumnIsCurrentSortAsc_FlipsToDesc()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("dir=desc", output.Content.GetContent());
    }

    [Fact]
    public void Process_WhenColumnIsCurrentSortDesc_FlipsToAsc()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "desc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("dir=asc", output.Content.GetContent());
    }

    [Fact]
    public void Process_WhenSortMatchesCaseInsensitively_TreatsAsActive()
    {
        var helper = BuildHelper("Title", currentSort: "title", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("dir=desc", output.Content.GetContent());
    }

    [Fact]
    public void Process_WhenActiveAsc_RendersUpArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("▲", output.Content.GetContent());
    }

    [Fact]
    public void Process_WhenActiveDesc_RendersDownArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "desc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("▼", output.Content.GetContent());
    }

    [Fact]
    public void Process_WhenInactive_RendersNoArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Year", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.DoesNotContain("▲", html);
        Assert.DoesNotContain("▼", html);
    }

    [Fact]
    public void Process_PreservesOtherQueryParams()
    {
        var helper = BuildHelper("Title", query: new() { ["page"] = "2", ["q"] = "wizard" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("page=2", html);
        Assert.Contains("q=wizard", html);
    }

    [Fact]
    public void Process_StripsExistingSortAndDirParams()
    {
        var helper = BuildHelper("Title", query: new() { ["sort"] = "Year", ["dir"] = "desc", ["page"] = "1" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "sort="));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "dir="));
        Assert.Contains("page=1", html);
    }

    [Fact]
    public void Process_StripsSortAndDirCaseInsensitively()
    {
        var helper = BuildHelper("Title", query: new() { ["SORT"] = "Year", ["DIR"] = "desc" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "sort="));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "dir="));
    }

    [Fact]
    public void Process_UriEscapesSortNameAndDirection()
    {
        var helper = BuildHelper("My Column");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("sort=My%20Column", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithoutChildContent_UsesNameAsLabel()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains(">Title", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithChildContent_UsesChildAsLabel()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext(childContent: "Book Title");

        helper.Process(context, output);

        Assert.Contains(">Book Title", output.Content.GetContent());
    }

    private static SortableColumnTagHelper BuildHelper(
        string name,
        string? currentSort = null,
        string? currentDirection = null,
        Dictionary<string, string>? query = null)
    {
        var httpContext = new DefaultHttpContext();
        if (query is not null)
        {
            httpContext.Request.Query = new QueryCollection(
                query.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Microsoft.Extensions.Primitives.StringValues(kvp.Value)));
        }

        return new SortableColumnTagHelper
        {
            Name = name,
            CurrentSort = currentSort,
            CurrentDirection = currentDirection,
            ViewContext = new ViewContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            }
        };
    }

    private static (TagHelperContext context, TagHelperOutput output) CreateContext(string? childContent = null)
    {
        var context = new TagHelperContext(
            "sortable-column",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            "sortable-column",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var content = new DefaultTagHelperContent();
                if (childContent is not null)
                {
                    content.Append(childContent);
                }
                return Task.FromResult<TagHelperContent>(content);
            });

        return (context, output);
    }
}
