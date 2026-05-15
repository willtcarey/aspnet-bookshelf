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
    [Fact]
    public void IsNumericTypeByteReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(byte)));
    }

    [Fact]
    public void IsNumericTypeShortReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(short)));
    }

    [Fact]
    public void IsNumericTypeIntReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(int)));
    }

    [Fact]
    public void IsNumericTypeLongReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(long)));
    }

    [Fact]
    public void IsNumericTypeFloatReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(float)));
    }

    [Fact]
    public void IsNumericTypeDoubleReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(double)));
    }

    [Fact]
    public void IsNumericTypeDecimalReturnsTrue()
    {
        Assert.True(FormTextTagHelper.IsNumericType(typeof(decimal)));
    }

    [Fact]
    public void IsNumericTypeNonNumericReturnsFalse()
    {
        Assert.False(FormTextTagHelper.IsNumericType(typeof(string)));
    }

    [Fact]
    public void ResolveInputTypeExplicitInputTypeOverridesAll()
    {
        var helper = BuildHelper(modelType: typeof(int), explicitInputType: "search");

        Assert.Equal("search", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeDataTypeIsKnownStringMapsToHtmlInputType()
    {
        var helper = BuildHelper(modelType: typeof(string), dataTypeName: "EmailAddress");

        Assert.Equal("email", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeNoExplicitNoDataTypeFallsBackToText()
    {
        var helper = BuildHelper(modelType: typeof(string));

        Assert.Equal("text", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeDataTypeIsPasswordMapsToPassword()
    {
        var helper = BuildHelper(modelType: typeof(string), dataTypeName: "Password");

        Assert.Equal("password", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeDataTypeIsUrlMapsToUrl()
    {
        var helper = BuildHelper(modelType: typeof(string), dataTypeName: "Url");

        Assert.Equal("url", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeDataTypeIsPhoneNumberMapsToTel()
    {
        var helper = BuildHelper(modelType: typeof(string), dataTypeName: "PhoneNumber");

        Assert.Equal("tel", helper.ResolveInputType());
    }

    [Fact]
    public void ResolveInputTypeNumericModelTypeFallsBackToNumber()
    {
        var helper = BuildHelper(modelType: typeof(int));

        Assert.Equal("number", helper.ResolveInputType());
    }

    [Fact]
    public void GenerateInputAppliesInputCssClass()
    {
        var generatedInput = new TagBuilder("input");
        var helper = BuildHelper(modelType: typeof(string), generatedInput: generatedInput);

        var result = helper.GenerateInput();

        Assert.Contains("input", result.Attributes["class"], StringComparison.Ordinal);
        Assert.Contains("w-full", result.Attributes["class"], StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessRendersFieldsetWithLabelInputAndValidation()
    {
        var helper = BuildHelper(modelType: typeof(string));
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

        Assert.Equal("fieldset", output.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    private static FormTextTagHelper BuildHelper(
        Type modelType,
        string? dataTypeName = null,
        string? explicitInputType = null,
        TagBuilder? generatedInput = null)
    {
        var generator = new Mock<IHtmlGenerator>();

        generator.Setup(g => g.GenerateTextBox(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(generatedInput ?? new TagBuilder("input"));

        generator.Setup(g => g.GenerateLabel(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("label"));

        generator.Setup(g => g.GenerateValidationMessage(
                It.IsAny<ViewContext>(), It.IsAny<ModelExplorer>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(new TagBuilder("span"));

        return new FormTextTagHelper(generator.Object, HtmlEncoder.Default)
        {
            For = BuildModelExpression(modelType, dataTypeName),
            ViewContext = TestViewContext.Create(),
            InputType = explicitInputType
        };
    }

    private static ModelExpression BuildModelExpression(Type modelType, string? dataTypeName = null)
    {
        var provider = dataTypeName is null
            ? (IModelMetadataProvider)new EmptyModelMetadataProvider()
            : new StubMetadataProvider(modelType, dataTypeName);

        var explorer = provider.GetModelExplorerForType(modelType, model: null);
        return new ModelExpression("Field", explorer);
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
