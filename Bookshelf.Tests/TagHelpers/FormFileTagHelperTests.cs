using System.Text.Encodings.Web;
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
    public void Process_WithHint_RendersHintLabel()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("data-upload-hint", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithoutHint_OmitsHintLabel()
    {
        var helper = BuildHelper(existingPath: null, hint: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.DoesNotContain("data-upload-hint", output.Content.GetContent());
    }

    [Fact]
    public void BuildPreviewContainer_WithExistingPath_RendersImageCard()
    {
        var container = FormFileTagHelper.BuildPreviewContainer("/uploads/cover.png");

        var html = RenderTag(container);
        Assert.Contains("<img", html);
        container.Attributes.TryGetValue("class", out var cls);
        Assert.DoesNotContain("hidden", cls ?? "");
    }

    [Fact]
    public void BuildPreviewContainer_WithoutExistingPath_RendersHiddenContainer()
    {
        var container = FormFileTagHelper.BuildPreviewContainer(null);

        Assert.Contains("hidden", container.Attributes["class"] ?? "");
        Assert.DoesNotContain("<img", RenderTag(container));
    }

    [Fact]
    public void BuildFileInput_WithAccept_AddsAcceptAttribute()
    {
        var helper = BuildHelper(existingPath: null, accept: "image/*");

        var input = helper.BuildFileInput();

        Assert.Equal("image/*", input.Attributes["accept"]);
    }

    [Fact]
    public void BuildFileInput_WithoutAccept_OmitsAcceptAttribute()
    {
        var helper = BuildHelper(existingPath: null, accept: null);

        var input = helper.BuildFileInput();

        Assert.False(input.Attributes.ContainsKey("accept"));
    }

    [Fact]
    public void BuildHiddenInput_WithPath_SetsValueToPath()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png");

        var input = helper.BuildHiddenInput("/uploads/cover.png");

        Assert.Equal("/uploads/cover.png", input.Attributes["value"]);
    }

    [Fact]
    public void BuildHiddenInput_NullPath_SetsEmptyValue()
    {
        var helper = BuildHelper(existingPath: null);

        var input = helper.BuildHiddenInput(null);

        Assert.Equal(string.Empty, input.Attributes["value"]);
    }

    [Fact]
    public void BuildHint_NotHidden_OmitsHiddenClass()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");

        var hint = helper.BuildHint(hidden: false);

        Assert.DoesNotContain("hidden", hint.Attributes["class"] ?? "");
    }

    [Fact]
    public void BuildHint_Hidden_AddsHiddenClass()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");

        var hint = helper.BuildHint(hidden: true);

        Assert.Contains("hidden", hint.Attributes["class"] ?? "");
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

        return new FormFileTagHelper(generator.Object, HtmlEncoder.Default)
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
