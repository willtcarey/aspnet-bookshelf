using Bookshelf.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Bookshelf.Tests.TagHelpers;

public class ImageUploadTagHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Process_WhenPathIsBlank_SuppressesOutput(string? path)
    {
        var helper = new ImageUploadTagHelper { Path = path! };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public void Process_WhenPathProvided_RendersImgTagWithSelfClosingMode()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/cover.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("img", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
    }

    [Fact]
    public void Process_WhenPathProvided_BuildsImagesUrl()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/cover.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/cover.png", GetAttribute(output, "src"));
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
    public void Process_WithWidth_AddsWidthQueryParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Width = 100 };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/x.png?w=100", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithHeight_AddsHeightQueryParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Height = 200 };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/x.png?h=200", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithFormat_AddsFormatQueryParam()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Format = "webp" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/x.png?format=webp", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithAllQueryParams_JoinsWithAmpersand()
    {
        var helper = new ImageUploadTagHelper
        {
            Path = "uploads/x.png",
            Width = 100,
            Height = 200,
            Format = "jpg"
        };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/x.png?w=100&h=200&format=jpg", GetAttribute(output, "src"));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 0)]
    public void Process_WithZeroDimension_OmitsDimensionQueryParam(int? width, int? height)
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/x.png", Width = width, Height = height };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/x.png", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithBackslashesInPath_NormalizesToForwardSlashes()
    {
        var helper = new ImageUploadTagHelper { Path = @"uploads\sub\cover.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/sub/cover.png", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithSpecialCharsInPathSegments_UriEscapesEachSegment()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads/my image (1).png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/my%20image%20%281%29.png", GetAttribute(output, "src"));
    }

    [Fact]
    public void Process_WithEmptySegmentsInPath_RemovesEmptySegments()
    {
        var helper = new ImageUploadTagHelper { Path = "uploads//cover.png" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("/images/uploads/cover.png", GetAttribute(output, "src"));
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
