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
        return Generator.GenerateTextBox(
            ViewContext, For.ModelExplorer, For.Name, For.Model,
            null, new { type = InputType ?? "text" });
    }
}
