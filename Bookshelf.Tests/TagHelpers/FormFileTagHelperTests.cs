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
    public void Process_RendersFieldsetWithDirectUploadAttribute()
    {
        var helper = BuildHelper(existingPath: null, uploadUrl: "/images/create");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Equal("fieldset", output.TagName);
        Assert.Equal("/images/create", output.Attributes["data-direct-upload"].Value);
    }

    [Fact]
    public void Process_RendersHiddenInputWithDirectUploadPath()
    {
        var helper = BuildHelper(existingPath: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        Assert.Contains("type=\"hidden\"", html);
        Assert.Contains("data-upload-path", html);
    }

    [Fact]
    public void Process_WithExistingPath_HiddenInputCarriesPathValue()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("value=\"/uploads/cover.png\"", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithoutExistingPath_HiddenInputHasEmptyValue()
    {
        var helper = BuildHelper(existingPath: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("value=\"\"", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithoutExistingPath_PreviewIsHidden()
    {
        var helper = BuildHelper(existingPath: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = output.Content.GetContent();
        var previewFragment = ExtractFragment(html, "data-upload-preview");
        Assert.Contains("hidden", previewFragment);
        Assert.DoesNotContain("<img", previewFragment);
    }

    [Fact]
    public void Process_WithExistingPath_PreviewShowsImageAndIsNotHidden()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var previewFragment = ExtractFragment(output.Content.GetContent(), "data-upload-preview");
        Assert.Contains("<img", previewFragment);
        Assert.DoesNotContain("hidden", previewFragment);
    }

    [Fact]
    public void Process_WithAcceptAttribute_PutsAcceptOnFileInput()
    {
        var helper = BuildHelper(existingPath: null, accept: "image/*");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("accept=\"image/*\"", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithoutAcceptAttribute_OmitsAcceptFromFileInput()
    {
        var helper = BuildHelper(existingPath: null, accept: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.DoesNotContain("accept=", output.Content.GetContent());
    }

    [Fact]
    public void Process_WithHintAndNoExistingPath_HintIsVisible()
    {
        var helper = BuildHelper(existingPath: null, hint: "Max 10 MB");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var hintFragment = ExtractFragment(output.Content.GetContent(), "data-upload-hint");
        Assert.Contains("Max 10 MB", hintFragment);
        Assert.DoesNotContain("hidden", hintFragment);
    }

    [Fact]
    public void Process_WithHintAndExistingPath_HintIsHidden()
    {
        var helper = BuildHelper(existingPath: "/uploads/cover.png", hint: "Max 10 MB");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var hintFragment = ExtractFragment(output.Content.GetContent(), "data-upload-hint");
        Assert.Contains("Max 10 MB", hintFragment);
        Assert.Contains("hidden", hintFragment);
    }

    [Fact]
    public void Process_WithoutHint_OmitsHintLabelEntirely()
    {
        var helper = BuildHelper(existingPath: null, hint: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.DoesNotContain("data-upload-hint", output.Content.GetContent());
    }

    [Fact]
    public void Process_AlwaysIncludesInitiallyHiddenErrorLabel()
    {
        var helper = BuildHelper(existingPath: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var errorFragment = ExtractFragment(output.Content.GetContent(), "data-upload-error");
        Assert.Contains("hidden", errorFragment);
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

    private static string ExtractFragment(string html, string marker)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return string.Empty;
        }

        var tagStart = html.LastIndexOf('<', markerIndex);
        if (tagStart < 0)
        {
            return string.Empty;
        }

        var tagNameEnd = html.IndexOfAny(new[] { ' ', '>', '/' }, tagStart + 1);
        if (tagNameEnd < 0)
        {
            return string.Empty;
        }

        var tagName = html.Substring(tagStart + 1, tagNameEnd - tagStart - 1);
        var closeTag = $"</{tagName}>";
        var closeIndex = html.IndexOf(closeTag, tagStart, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            return string.Empty;
        }

        return html.Substring(tagStart, closeIndex - tagStart + closeTag.Length);
    }

}
