using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-text", Attributes = "asp-for")]
public class FormTextTagHelper : FormTagHelperBase
{
    [HtmlAttributeName("type")]
    public string? InputType { get; set; }

    public FormTextTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    protected override TagBuilder GenerateInput()
    {
        var inputTag = Generator.GenerateTextBox(
            ViewContext, For.ModelExplorer, For.Name, For.Model,
            null, new { type = ResolveInputType() });

        inputTag.AddCssClass("input w-full");
        return inputTag;
    }

    private string ResolveInputType()
    {
        if (!string.IsNullOrWhiteSpace(InputType))
        {
            return InputType;
        }

        var dataType = For.Metadata.DataTypeName?.ToUpperInvariant();
        return dataType switch
        {
            "PASSWORD" => "password",
            "EMAILADDRESS" => "email",
            "URL" => "url",
            "PHONENUMBER" => "tel",
            _ when IsNumericType(For.ModelExplorer.ModelType) => "number",
            _ => "text"
        };
    }

    private static bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(byte)
            || underlyingType == typeof(short)
            || underlyingType == typeof(int)
            || underlyingType == typeof(long)
            || underlyingType == typeof(float)
            || underlyingType == typeof(double)
            || underlyingType == typeof(decimal);
    }
}
