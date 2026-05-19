using Bookshelf.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Bookshelf.Tests.TagHelpers;

public class SortableColumnTagHelperTests
{
    [Fact]
    public void ProcessRendersThElement()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("th", output.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    [Fact]
    public void ProcessNotCurrentSortBuildsAscLinkAndNoArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Year", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("sort=Title", html, StringComparison.Ordinal);
        Assert.Contains("dir=asc", html, StringComparison.Ordinal);
        Assert.DoesNotContain("▲", html, StringComparison.Ordinal);
        Assert.DoesNotContain("▼", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessCurrentSortAscFlipsToDescAndRendersUpArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("dir=desc", html, StringComparison.Ordinal);
        Assert.Contains("▲", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessCurrentSortDescFlipsToAscAndRendersDownArrow()
    {
        var helper = BuildHelper("Title", currentSort: "Title", currentDirection: "desc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("dir=asc", html, StringComparison.Ordinal);
        Assert.Contains("▼", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessSortMatchesCaseInsensitivelyTreatsAsActive()
    {
        var helper = BuildHelper("Title", currentSort: "title", currentDirection: "asc");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("dir=desc", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessQueryParamWithNullValueEmitsEmptyString()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["filter"] = new Microsoft.Extensions.Primitives.StringValues(new string?[] { null })
        });
        var helper = new SortableColumnTagHelper
        {
            Name = "Title",
            ViewContext = new ViewContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            }
        };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("filter=", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessPreservesOtherQueryParams()
    {
        var helper = BuildHelper("Title", query: new() { ["page"] = "2", ["q"] = "wizard" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("page=2", html, StringComparison.Ordinal);
        Assert.Contains("q=wizard", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessStripsExistingSortAndDirParams()
    {
        var helper = BuildHelper("Title", query: new() { ["sort"] = "Year", ["dir"] = "desc", ["page"] = "1" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "sort="));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "dir="));
    }

    [Fact]
    public void ProcessStripsSortAndDirCaseInsensitively()
    {
        var helper = BuildHelper("Title", query: new() { ["SORT"] = "Year", ["DIR"] = "desc" });
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "sort="));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "dir="));
    }

    [Fact]
    public void ProcessUriEscapesSortName()
    {
        var helper = BuildHelper("My Column");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("sort=My%20Column", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithoutChildContentUsesNameAsLabel()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains(">Title", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithChildContentUsesChildAsLabel()
    {
        var helper = BuildHelper("Title");
        var (context, output) = CreateContext(childContent: "Book Title");

        helper.Process(context, output);

        Assert.Contains(">Book Title", output.Content.GetContent(), StringComparison.Ordinal);
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
