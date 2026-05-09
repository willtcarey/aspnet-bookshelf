using Bookshelf.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Bookshelf.Tests.TagHelpers;

public class ImageUploadTagHelperTests
{
    [Fact]
    public void Process_BlankPath_SuppressesOutput()
    {
        var helper = new ImageUploadTagHelper { Path = "" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public void Process_WithPath_RendersImgTag()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/cover.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("img", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
    }

    [Fact]
    public void Process_WithAlt_SetsAltAttribute()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Alt = "Cover" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("Cover", GetAttribute(output, "alt"));
    }

    [Fact]
    public void Process_WithoutAlt_SetsEmptyAlt()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal(string.Empty, GetAttribute(output, "alt"));
    }

    [Fact]
    public void BuildSource_NoQueryParams_OmitsQueryString()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/cover.png" };

        Assert.Equal("/images/uploads/cover.png", helper.BuildSource());
    }

    [Fact]
    public void BuildSource_WithWidth_AddsWidthParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Width = 100 };

        Assert.Equal("/images/uploads/x.png?w=100", helper.BuildSource());
    }

    [Fact]
    public void BuildSource_WithHeight_AddsHeightParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Height = 200 };

        Assert.Equal("/images/uploads/x.png?h=200", helper.BuildSource());
    }

    [Fact]
    public void BuildSource_WithFormat_AddsFormatParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Format = "webp" };

        Assert.Equal("/images/uploads/x.png?format=webp", helper.BuildSource());
    }

    [Fact]
    public void BuildSource_WithBackslashesInPath_NormalizesToForwardSlashes()
    {
        var helper = new ImageUploadTagHelper { Path = @"uploads\sub\cover.png" };

        Assert.Equal("/images/uploads/sub/cover.png", helper.BuildSource());
    }

    private static (TagHelperContext context, TagHelperOutput output) CreateContext()
    {
        var context = new TagHelperContext(
            "image-upload",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            "image-upload",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (context, output);
    }

    private static string? GetAttribute(TagHelperOutput output, string name)
    {
        return output.Attributes.TryGetAttribute(name, out var attribute)
            ? attribute.Value?.ToString()
            : null;
    }
}
