using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-select", Attributes = "asp-for")]
public class FormSelectTagHelper : FormTagHelperBase
{
    [HtmlAttributeName("asp-items")]
    public SelectList? Items { get; set; }

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    public FormSelectTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    protected override TagBuilder GenerateInput()
    {
        var selectTag = Generator.GenerateSelect(
            ViewContext, For.ModelExplorer, null, For.Name, Items,
            allowMultiple: false, htmlAttributes: null);

        if (!string.IsNullOrEmpty(Placeholder))
        {
            var placeholderOption = new TagBuilder("option");
            placeholderOption.Attributes["value"] = "";
            placeholderOption.InnerHtml.Append(Placeholder);
            selectTag.InnerHtml.Prepend(placeholderOption);
        }

        return selectTag;
    }
}
