using System.Text.Encodings.Web;
using Bookshelf.TagHelpers;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Bookshelf.Tests.TagHelpers;

public class FormTextTagHelperTests
{
    [Theory]
    [InlineData(typeof(byte), "number")]
    [InlineData(typeof(short), "number")]
    [InlineData(typeof(int), "number")]
    [InlineData(typeof(long), "number")]
    [InlineData(typeof(float), "number")]
    [InlineData(typeof(double), "number")]
    [InlineData(typeof(decimal), "number")]
    [InlineData(typeof(string), "text")]
    [InlineData(typeof(int?), "number")]
    [InlineData(typeof(decimal?), "number")]
    public void Process_NumericModelTypes_ResolveToNumberOrText(Type modelType, string expectedType)
    {
        var capturedType = RunProcessAndCaptureInputType(modelType: modelType);

        Assert.Equal(expectedType, capturedType);
    }

    [Fact]
    public void Process_ExplicitInputType_OverridesAllInference()
    {
        var capturedType = RunProcessAndCaptureInputType(
            modelType: typeof(int),
            explicitInputType: "search");

        Assert.Equal("search", capturedType);
    }

    [Theory]
    [InlineData("Password", "password")]
    [InlineData("EmailAddress", "email")]
    [InlineData("Url", "url")]
    [InlineData("PhoneNumber", "tel")]
    public void Process_StringModelWithDataType_MapsToHtmlInputType(string dataTypeName, string expectedType)
    {
        var capturedType = RunProcessAndCaptureInputType(
            modelType: typeof(string),
            dataTypeName: dataTypeName);

        Assert.Equal(expectedType, capturedType);
    }

    [Fact]
    public void Process_StringModelWithNoDataType_FallsBackToText()
    {
        var capturedType = RunProcessAndCaptureInputType(modelType: typeof(string));

        Assert.Equal("text", capturedType);
    }

    [Fact]
    public void Process_GeneratedInput_HasInputCssClass()
    {
        var generator = BuildGeneratorMock();
        var generatedTag = new TagBuilder("input");
        generator.Setup(g => g.GenerateTextBox(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer?>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(generatedTag);

        var helper = new FormTextTagHelper(generator.Object, HtmlEncoder.Default)
        {
            For = BuildModelExpression(typeof(string)),
            ViewContext = TestViewContext.Create()
        };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        Assert.Contains("input", generatedTag.Attributes["class"] ?? "");
        Assert.Contains("w-full", generatedTag.Attributes["class"] ?? "");
    }

    private static string? RunProcessAndCaptureInputType(
        Type modelType,
        string? dataTypeName = null,
        string? explicitInputType = null)
    {
        var generator = BuildGeneratorMock();
        object? capturedHtmlAttributes = null;

        generator.Setup(g => g.GenerateTextBox(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer?>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Callback<ViewContext, ModelExplorer?, string, object?, string?, object?>(
                (_, _, _, _, _, attrs) => capturedHtmlAttributes = attrs)
            .Returns(new TagBuilder("input"));

        var helper = new FormTextTagHelper(generator.Object, HtmlEncoder.Default)
        {
            For = BuildModelExpression(modelType, dataTypeName),
            ViewContext = TestViewContext.Create(),
            InputType = explicitInputType
        };
        var (context, output) = CreateContext();

        helper.Process(context, output);

        return capturedHtmlAttributes?
            .GetType()
            .GetProperty("type")?
            .GetValue(capturedHtmlAttributes) as string;
    }

    private static Mock<IHtmlGenerator> BuildGeneratorMock()
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

        return generator;
    }

    private static ModelExpression BuildModelExpression(Type modelType, string? dataTypeName = null)
    {
        var provider = dataTypeName is null
            ? (IModelMetadataProvider)new EmptyModelMetadataProvider()
            : new StubMetadataProvider(modelType, dataTypeName);

        var explorer = provider.GetModelExplorerForType(modelType, model: null);
        return new ModelExpression("Field", explorer);
    }

    private static (TagHelperContext context, TagHelperOutput output) CreateContext()
    {
        var context = new TagHelperContext(
            "form-text",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            "form-text",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (context, output);
    }

    private sealed class StubMetadataProvider : EmptyModelMetadataProvider
    {
        private readonly Type _targetType;
        private readonly string _dataTypeName;

        public StubMetadataProvider(Type targetType, string dataTypeName)
        {
            _targetType = targetType;
            _dataTypeName = dataTypeName;
        }

        public override ModelMetadata GetMetadataForType(Type modelType)
        {
            var baseMetadata = base.GetMetadataForType(modelType);
            return modelType == _targetType
                ? new MetadataWithDataType(baseMetadata, _dataTypeName)
                : baseMetadata;
        }
    }

    private sealed class MetadataWithDataType : ModelMetadata
    {
        private readonly ModelMetadata _inner;
        private readonly string _dataTypeName;

        public MetadataWithDataType(ModelMetadata inner, string dataTypeName)
            : base(ModelMetadataIdentity.ForType(inner.ModelType))
        {
            _inner = inner;
            _dataTypeName = dataTypeName;
        }

        public override string? DataTypeName => _dataTypeName;

        public override IReadOnlyDictionary<object, object> AdditionalValues => _inner.AdditionalValues;
        public override ModelPropertyCollection Properties => _inner.Properties;
        public override string? BinderModelName => _inner.BinderModelName;
        public override Type? BinderType => _inner.BinderType;
        public override Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource? BindingSource => _inner.BindingSource;
        public override bool ConvertEmptyStringToNull => _inner.ConvertEmptyStringToNull;
        public override string? Description => _inner.Description;
        public override string? DisplayFormatString => _inner.DisplayFormatString;
        public override string? DisplayName => _inner.DisplayName;
        public override string? EditFormatString => _inner.EditFormatString;
        public override ModelMetadata? ElementMetadata => _inner.ElementMetadata;
        public override IEnumerable<KeyValuePair<EnumGroupAndName, string>>? EnumGroupedDisplayNamesAndValues => _inner.EnumGroupedDisplayNamesAndValues;
        public override IReadOnlyDictionary<string, string>? EnumNamesAndValues => _inner.EnumNamesAndValues;
        public override bool HasNonDefaultEditFormat => _inner.HasNonDefaultEditFormat;
        public override bool HtmlEncode => _inner.HtmlEncode;
        public override bool HideSurroundingHtml => _inner.HideSurroundingHtml;
        public override bool IsBindingAllowed => _inner.IsBindingAllowed;
        public override bool IsBindingRequired => _inner.IsBindingRequired;
        public override bool IsEnum => _inner.IsEnum;
        public override bool IsFlagsEnum => _inner.IsFlagsEnum;
        public override bool IsReadOnly => _inner.IsReadOnly;
        public override bool IsRequired => _inner.IsRequired;
        public override ModelBindingMessageProvider ModelBindingMessageProvider => _inner.ModelBindingMessageProvider;
        public override int Order => _inner.Order;
        public override string? Placeholder => _inner.Placeholder;
        public override string? NullDisplayText => _inner.NullDisplayText;
        public override IPropertyFilterProvider? PropertyFilterProvider => _inner.PropertyFilterProvider;
        public override bool ShowForDisplay => _inner.ShowForDisplay;
        public override bool ShowForEdit => _inner.ShowForEdit;
        public override string? SimpleDisplayProperty => _inner.SimpleDisplayProperty;
        public override string? TemplateHint => _inner.TemplateHint;
        public override bool ValidateChildren => _inner.ValidateChildren;
        public override IReadOnlyList<object> ValidatorMetadata => _inner.ValidatorMetadata;
        public override Func<object, object?>? PropertyGetter => _inner.PropertyGetter;
        public override Action<object, object?>? PropertySetter => _inner.PropertySetter;
    }

}
