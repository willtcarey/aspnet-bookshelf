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
    [Fact]
    public void ProcessExplicitInputTypeOverridesAll()
    {
        var build = BuildHelper(modelType: typeof(int), explicitInputType: "search");

        Process(build.Helper);

        AssertGeneratedInputType(build, "search");
    }

    [Theory]
    [InlineData("Password", "password")]
    [InlineData("EmailAddress", "email")]
    [InlineData("Url", "url")]
    [InlineData("PhoneNumber", "tel")]
    public void ProcessKnownDataTypeMapsToHtmlInputType(string dataTypeName, string expectedInputType)
    {
        var build = BuildHelper(modelType: typeof(string), dataTypeName: dataTypeName);

        Process(build.Helper);

        AssertGeneratedInputType(build, expectedInputType);
    }

    [Fact]
    public void ProcessNoExplicitNoDataTypeFallsBackToText()
    {
        var build = BuildHelper(modelType: typeof(string));

        Process(build.Helper);

        AssertGeneratedInputType(build, "text");
    }

    [Theory]
    [MemberData(nameof(NumericModelTypes))]
    public void ProcessNumericModelTypeFallsBackToNumber(Type modelType)
    {
        var build = BuildHelper(modelType: modelType);

        Process(build.Helper);

        AssertGeneratedInputType(build, "number");
    }

    [Fact]
    public void ProcessAppliesInputCssClass()
    {
        var generatedInput = new TagBuilder("input");
        var build = BuildHelper(modelType: typeof(string), generatedInput: generatedInput);

        Process(build.Helper);

        Assert.Contains("input", generatedInput.Attributes["class"], StringComparison.Ordinal);
        Assert.Contains("w-full", generatedInput.Attributes["class"], StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessRendersFieldsetWithLabelInputAndValidation()
    {
        var build = BuildHelper(modelType: typeof(string));

        var output = Process(build.Helper);

        Assert.Equal("fieldset", output.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    [Fact]
    public void ProcessRendersFieldsetViaBaseRenderContent()
    {
        // Exercises FormTagHelperBase.Process AND the default RenderContent
        // implementation (FormTextTagHelper doesn't override RenderContent).
        var build = BuildHelper(modelType: typeof(string));
        var output = new TagHelperOutput(
            "form-text",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        build.Helper.Process(
            new TagHelperContext("form-text", new TagHelperAttributeList(), new Dictionary<object, object>(), "x"),
            output);

        Assert.Equal("fieldset", output.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    public static TheoryData<Type> NumericModelTypes => new()
    {
        typeof(byte),
        typeof(short),
        typeof(int),
        typeof(int?),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    private static HelperBuildResult BuildHelper(
        Type modelType,
        string? dataTypeName = null,
        string? explicitInputType = null,
        TagBuilder? generatedInput = null)
    {
        var generator = new Mock<IHtmlGenerator>();
        var capturedHtmlAttributes = (object?)null;

        generator.Setup(g => g.GenerateTextBox(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Callback<ViewContext, ModelExplorer, string, object?, string?, object?>(
                (_, _, _, _, _, htmlAttributes) => capturedHtmlAttributes = htmlAttributes)
            .Returns(generatedInput ?? new TagBuilder("input"));

        generator.Setup(g => g.GenerateLabel(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("label"));

        generator.Setup(g => g.GenerateValidationMessage(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("span"));

        var helper = new FormTextTagHelper(generator.Object)
        {
            For = BuildModelExpression(modelType, dataTypeName),
            ViewContext = TestViewContext.Create(),
            InputType = explicitInputType
        };

        return new HelperBuildResult(helper, () => capturedHtmlAttributes);
    }

    private static TagHelperOutput Process(FormTextTagHelper helper)
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

        helper.Process(context, output);
        return output;
    }

    private static void AssertGeneratedInputType(HelperBuildResult build, string expectedInputType)
    {
        var htmlAttributes = build.GetGeneratedHtmlAttributes();
        Assert.NotNull(htmlAttributes);
        Assert.Equal(expectedInputType, GetPropertyValue(htmlAttributes, "type"));
    }

    private static object? GetPropertyValue(object value, string propertyName)
    {
        return value.GetType().GetProperty(propertyName)?.GetValue(value);
    }

    private static ModelExpression BuildModelExpression(Type modelType, string? dataTypeName = null)
    {
        var provider = dataTypeName is null
            ? (IModelMetadataProvider)new EmptyModelMetadataProvider()
            : new StubMetadataProvider(modelType, dataTypeName);

        var explorer = provider.GetModelExplorerForType(modelType, model: null);
        return new ModelExpression("Field", explorer);
    }

    private sealed record HelperBuildResult(
        FormTextTagHelper Helper,
        Func<object?> GetGeneratedHtmlAttributes);

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
        public override BindingSource? BindingSource => _inner.BindingSource;
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
