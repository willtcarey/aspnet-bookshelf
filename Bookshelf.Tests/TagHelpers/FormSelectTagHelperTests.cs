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
    public void GenerateInput_NoPlaceholder_DoesNotPrependPlaceholderOption()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: null, modelValue: 5);

        helper.GenerateInput();

        Assert.DoesNotContain("disabled", RenderInnerHtml(generatedSelect));
    }

    [Fact]
    public void GenerateInput_WithPlaceholderAndNonBlankModel_PrependsButDoesNotSelectPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: 5);

        helper.GenerateInput();

        var html = RenderInnerHtml(generatedSelect);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("selected", html);
    }

    [Fact]
    public void GenerateInput_WithPlaceholderAndBlankModel_PrependsAndSelectsPlaceholder()
    {
        var generatedSelect = new TagBuilder("select");
        var helper = BuildHelper(generatedSelect, placeholder: "Pick one", modelValue: 0);

        helper.GenerateInput();

        Assert.Contains("selected", RenderInnerHtml(generatedSelect));
    }

    [Fact]
    public void ShouldSelectPlaceholder_ModelIsNullOrWhitespace_ReturnsTrue()
    {
        var helper = BuildHelper(new TagBuilder("select"), placeholder: "X", modelValue: null);

        Assert.True(helper.ShouldSelectPlaceholder());
    }

    [Fact]
    public void ShouldSelectPlaceholder_ModelIsZero_ReturnsTrue()
    {
        var helper = BuildHelper(new TagBuilder("select"), placeholder: "X", modelValue: "0");

        Assert.True(helper.ShouldSelectPlaceholder());
    }

    [Fact]
    public void ShouldSelectPlaceholder_ModelIsNonBlankNonZero_ReturnsFalse()
    {
        var helper = BuildHelper(new TagBuilder("select"), placeholder: "X", modelValue: "hello");

        Assert.False(helper.ShouldSelectPlaceholder());
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
}
