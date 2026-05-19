using Bookshelf.Services;
using Bookshelf.TagHelpers;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Bookshelf.Tests.TagHelpers;

public class FormFileTagHelperTests
{
    [Fact]
    public void ProcessWithoutHintOmitsHintLabel()
    {
        var helper = BuildHelper(existingPath: null, hint: null);

        var html = ProcessAndRenderContent(helper);

        Assert.DoesNotContain("data-upload-hint", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithExistingPathRendersImagePreviewCard()
    {
        const string storedPath = "/uploads/11111111111111111111111111111111.png";
        var helper = BuildHelper(existingPath: storedPath);

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("<img", html, StringComparison.Ordinal);
        Assert.Contains("Current image", html, StringComparison.Ordinal);
        Assert.Contains("/images/11111111111111111111111111111111.png?w=64&amp;h=96", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithoutExistingPathRendersEmptyPreviewContainer()
    {
        var helper = BuildHelper(existingPath: null);

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("data-upload-preview", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Current image", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithAcceptAddsAcceptAttribute()
    {
        var helper = BuildHelper(existingPath: null, accept: "image/*");

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("accept=\"image/*\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithExistingPathSetsHiddenInputValueToPath()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png");

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"/uploads/cover.png\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithNullPathSetsHiddenInputValueToEmptyString()
    {
        var helper = BuildHelper(existingPath: null);

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithHintAndNoExistingImageShowsHint()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");

        var html = ProcessAndRenderContent(helper);

        Assert.DoesNotContain("hidden", StartTagContaining(html, "data-upload-hint"), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithHintAndExistingImageHidesHint()
    {
        var helper = BuildHelper(
            existingPath: "/uploads/11111111111111111111111111111111.png",
            hint: "Max 10 MB");

        var html = ProcessAndRenderContent(helper);

        Assert.Contains("hidden", StartTagContaining(html, "data-upload-hint"), StringComparison.Ordinal);
    }

    private static FormFileTagHelper BuildHelper(
        string? existingPath,
        string? accept = null,
        string? hint = null,
        string uploadUrl = "/images/create")
    {
        var generator = new Mock<IHtmlGenerator>();

        generator.Setup(g => g.GenerateLabel(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("label"));

        generator.Setup(g => g.GenerateValidationMessage(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("span"));

        var provider = new EmptyModelMetadataProvider();
        var explorer = provider.GetModelExplorerForType(typeof(string), existingPath);

        var paths = TestUploadPaths.Create(Path.GetTempPath());
        var imageStorage = new ImageStorage(paths);

        return new FormFileTagHelper(generator.Object, imageStorage)
        {
            For = new ModelExpression("CoverImagePath", explorer),
            ViewContext = TestViewContext.Create(),
            Accept = accept,
            Hint = hint,
            UploadUrl = uploadUrl
        };
    }

    private static string ProcessAndRenderContent(FormFileTagHelper helper)
    {
        var (context, output) = CreateContext();

        helper.Process(context, output);

        return output.Content.GetContent();
    }

    private static (TagHelperContext context, TagHelperOutput output) CreateContext()
    {
        var context = new TagHelperContext(
            "form-file",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            "form-file",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (context, output);
    }

    private static string StartTagContaining(string html, string marker)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Expected rendered HTML to contain '{marker}'.");

        var start = html.LastIndexOf('<', markerIndex);
        var end = html.IndexOf('>', markerIndex);
        Assert.True(start >= 0 && end >= start, $"Expected rendered HTML marker '{marker}' to be inside a tag.");

        return html[start..(end + 1)];
    }
}
