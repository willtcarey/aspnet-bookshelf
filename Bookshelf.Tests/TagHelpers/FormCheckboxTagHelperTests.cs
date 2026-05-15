using System.Text.Encodings.Web;
using Bookshelf.TagHelpers;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Bookshelf.Tests.TagHelpers;

public class FormCheckboxTagHelperTests
{
    [Fact]
    public void GenerateInputAppliesCheckboxCssClasses()
    {
        var helper = BuildHelper();

        var input = helper.GenerateInput();

        Assert.Contains("checkbox", input.Attributes["class"], StringComparison.Ordinal);
        Assert.Contains("checkbox-primary", input.Attributes["class"], StringComparison.Ordinal);
    }

    [Fact]
    public void EncoderPropertyExposesInjectedEncoder()
    {
        var encoder = HtmlEncoder.Default;
        var helper = new EncoderExposingHelper(Mock.Of<IHtmlGenerator>(), encoder);

        Assert.Same(encoder, helper.GetEncoder());
    }

    private sealed class EncoderExposingHelper : FormTagHelperBase
    {
        public EncoderExposingHelper(IHtmlGenerator generator, HtmlEncoder encoder)
            : base(generator, encoder) { }

        public HtmlEncoder GetEncoder() => Encoder;

        protected internal override TagBuilder GenerateInput() => new("input");
    }

    [Fact]
    public void ProcessRendersFieldsetWithCheckboxRow()
    {
        var helper = BuildHelper();
        var context = new TagHelperContext(
            "form-checkbox",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(
            "form-checkbox",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(context, output);

        Assert.Equal("fieldset", output.TagName);
        var rendered = output.Content.GetContent();
        Assert.Contains("flex items-center gap-3", rendered, StringComparison.Ordinal);
    }

    private static FormCheckboxTagHelper BuildHelper()
    {
        var generator = new Mock<IHtmlGenerator>();

        generator.Setup(g => g.GenerateCheckBox(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<bool?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("input"));

        generator.Setup(g => g.GenerateLabel(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("label"));

        generator.Setup(g => g.GenerateValidationMessage(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("span"));

        var provider = new EmptyModelMetadataProvider();
        var explorer = provider.GetModelExplorerForType(typeof(bool), model: false);

        return new FormCheckboxTagHelper(generator.Object, HtmlEncoder.Default)
        {
            For = new ModelExpression("Active", explorer),
            ViewContext = TestViewContext.Create()
        };
    }
}
