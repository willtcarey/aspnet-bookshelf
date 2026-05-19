using Bookshelf.Services;
using Bookshelf.TagHelpers;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Bookshelf.Tests.TagHelpers;

public class ImageUploadTagHelperTests
{
    private const string ValidStoredPath = "/uploads/11111111111111111111111111111111.png";

    private static ImageStorage CreateImageStorage()
    {
        var paths = TestUploadPaths.Create(Path.GetTempPath());
        return new ImageStorage(paths);
    }

    [Fact]
    public void ProcessBlankPathSuppressesOutput()
    {
        var helper = new ImageUploadTagHelper(CreateImageStorage()) { Path = "" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public void ProcessWithPathRendersImgTag()
    {
        var helper = new ImageUploadTagHelper(CreateImageStorage()) { Path = ValidStoredPath };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("img", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
    }

    [Fact]
    public void ProcessWithAltSetsAltAttribute()
    {
        var helper = new ImageUploadTagHelper(CreateImageStorage()) { Path = ValidStoredPath, Alt = "Cover" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("Cover", GetAttribute(output, "alt"));
    }

    [Fact]
    public void ProcessWithoutAltSetsEmptyAlt()
    {
        var helper = new ImageUploadTagHelper(CreateImageStorage()) { Path = ValidStoredPath };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal(string.Empty, GetAttribute(output, "alt"));
    }

    [Fact]
    public void ProcessInvalidPathSuppressesOutput()
    {
        var helper = new ImageUploadTagHelper(CreateImageStorage()) { Path = "has/slash" };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Null(output.TagName);
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
