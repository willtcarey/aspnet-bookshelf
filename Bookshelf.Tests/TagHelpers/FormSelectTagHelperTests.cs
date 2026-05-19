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
    public void ProcessNoPlaceholderDoesNotPrependPlaceholderOption()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: null, modelValue: 5);

        Process(helper);

        Assert.DoesNotContain("disabled", RenderInnerHtml(generatedSelect), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithPlaceholderAndBlankModelPrependsAndSelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: null);

        Process(helper);

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("disabled", html, StringComparison.Ordinal);
        Assert.Contains("selected", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithPlaceholderAndZeroModelSelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: "0");

        Process(helper);

        Assert.Contains("selected", RenderInnerHtml(generatedSelect), StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessWithPlaceholderAndNonZeroStringModelDoesNotSelectPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: "hello");

        Process(helper);

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("disabled", html, StringComparison.Ordinal);
        Assert.DoesNotContain("selected", html, StringComparison.Ordinal);
    }

    private static FormSelectTagHelper BuildHelper(
        TagBuilder generatedSelect,
        string? placeholder,
        object? modelValue)
    {
        var generator = new Mock<IHtmlGenerator>();

        generator.Setup(g => g.GenerateSelect(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<SelectListItem>?>(),
                It.IsAny<bool>(), It.IsAny<object?>()))
            .Returns(generatedSelect);

        generator.Setup(g => g.GenerateLabel(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("label"));

        generator.Setup(g => g.GenerateValidationMessage(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("span"));

        var modelType = modelValue?.GetType() ?? typeof(int?);
        var provider = new EmptyModelMetadataProvider();
        var explorer = provider.GetModelExplorerForType(modelType, modelValue);

        return new FormSelectTagHelper(generator.Object)
        {
            For = new ModelExpression("Field", explorer),
            ViewContext = TestViewContext.Create(),
            Placeholder = placeholder
        };
    }

    private static void Process(FormSelectTagHelper helper)
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

        helper.Process(context, output);
    }

    private static string RenderInnerHtml(TagBuilder tag)
    {
        using var writer = new StringWriter();
        tag.InnerHtml.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
