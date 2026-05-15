using System.Text.Encodings.Web;
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
    public void ProcessWithHintRendersHintLabel()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("data-upload-hint", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithoutHintOmitsHintLabel()
    {
        var helper = BuildHelper(existingPath: null, hint: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.DoesNotContain("data-upload-hint", output.Content.GetContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPreviewContainerWithExistingPathRendersImageCard()
    {
        const string storedPath = "/uploads/11111111111111111111111111111111.png";
        var helper = BuildHelper(existingPath: storedPath);

        var container = helper.BuildPreviewContainer(storedPath);

        var html = RenderTag(container);
        Assert.Contains("<img", html, StringComparison.Ordinal);
        container.Attributes.TryGetValue("class", out var cls);
        Assert.DoesNotContain("hidden", cls ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPreviewContainerWithoutExistingPathRendersHiddenContainer()
    {
        var helper = BuildHelper(existingPath: null);

        var container = helper.BuildPreviewContainer(null);

        Assert.Contains("hidden", container.Attributes["class"] ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("<img", RenderTag(container), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFileInputWithAcceptAddsAcceptAttribute()
    {
        var helper = BuildHelper(existingPath: null, accept: "image/*");

        var input = helper.BuildFileInput();

        Assert.Equal("image/*", input.Attributes["accept"]);
    }

    [Fact]
    public void BuildFileInputWithoutAcceptOmitsAcceptAttribute()
    {
        var helper = BuildHelper(existingPath: null, accept: null);

        var input = helper.BuildFileInput();

        Assert.False(input.Attributes.ContainsKey("accept"));
    }

    [Fact]
    public void BuildHiddenInputWithPathSetsValueToPath()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png");

        var input = helper.BuildHiddenInput("/uploads/cover.png");

        Assert.Equal("/uploads/cover.png", input.Attributes["value"]);
    }

    [Fact]
    public void BuildHiddenInputNullPathSetsEmptyValue()
    {
        var helper = BuildHelper(existingPath: null);

        var input = helper.BuildHiddenInput(null);

        Assert.Equal(string.Empty, input.Attributes["value"]);
    }

    [Fact]
    public void BuildHintNotHiddenOmitsHiddenClass()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");

        var hint = helper.BuildHint(hidden: false);

        Assert.DoesNotContain("hidden", hint.Attributes["class"] ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHintHiddenAddsHiddenClass()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");

        var hint = helper.BuildHint(hidden: true);

        Assert.Contains("hidden", hint.Attributes["class"] ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateInputReturnsPlaceholderInputTag()
    {
        var helper = BuildHelper(existingPath: null);

        var input = helper.GenerateInput();

        Assert.NotNull(input);
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

        return new FormFileTagHelper(generator.Object, HtmlEncoder.Default, imageStorage)
        {
            For = new ModelExpression("CoverImagePath", explorer),
            ViewContext = TestViewContext.Create(),
            Accept = accept,
            Hint = hint,
            UploadUrl = uploadUrl
        };
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

    private static string RenderTag(TagBuilder tag)
    {
        using var writer = new StringWriter();
        tag.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
