using System.Text.Encodings.Web;
using Bookshelf.TagHelpers;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Bookshelf.Tests.TagHelpers;

public class FormSelectTagHelperTests
{
    [Fact]
    public void Process_WithoutPlaceholder_DoesNotPrependPlaceholderOption()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: null, modelValue: 5);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.DoesNotContain("disabled", RenderInnerHtml(generatedSelect));
    }

    [Fact]
    public void Process_WithPlaceholderAndNonZeroModel_PrependsButDoesNotSelectPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: 5);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("Pick one", html);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("selected", html);
    }

    [Fact]
    public void Process_WithPlaceholderAndZeroModel_SelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: 0);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("Pick one", html);
        Assert.Contains("selected", html);
    }

    [Fact]
    public void Process_WithPlaceholderAndNullModel_SelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: null);
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("selected", RenderInnerHtml(generatedSelect));
    }

    [Fact]
    public void Process_WithPlaceholderAndEmptyStringModel_SelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: "");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("selected", RenderInnerHtml(generatedSelect));
    }

    [Fact]
    public void Process_WithPlaceholderAndStringModel_DoesNotSelectPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: "hello");
        var (context, output) = CreateContext();

        helper.Process(context, output);

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("selected", html);
    }

    private static FormSelectTagHelper BuildHelper(
        TagBuilder generatedSelect,
        string? placeholder,
        object? modelValue)
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

        generator.Setup(g => g.GenerateSelect(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<SelectListItem>?>(),
                It.IsAny<bool>(), It.IsAny<object?>()))
            .Returns(generatedSelect);

        var modelType = modelValue?.GetType() ?? typeof(int?);
        var provider = new EmptyModelMetadataProvider();
        var explorer = provider.GetModelExplorerForType(modelType, modelValue);

        return new FormSelectTagHelper(generator.Object, HtmlEncoder.Default)
        {
            For = new ModelExpression("Field", explorer),
            ViewContext = TestViewContext.Create(),
            Placeholder = placeholder
        };
    }

    private static string RenderInnerHtml(TagBuilder tag)
    {
        using var writer = new StringWriter();
        tag.InnerHtml.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static (TagHelperContext context, TagHelperOutput output) CreateContext()
    {
        var context = new TagHelperContext(
            "form-select",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            "form-select",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (context, output);
    }

}
