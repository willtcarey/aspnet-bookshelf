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
            allowMultiple: false,
            htmlAttributes: new { @class = "select select-bordered w-full" });

        if (!string.IsNullOrEmpty(Placeholder))
        {
            var placeholderOption = new TagBuilder("option");
            placeholderOption.Attributes["value"] = "";
            placeholderOption.Attributes["disabled"] = "disabled";

            if (ShouldSelectPlaceholder())
            {
                placeholderOption.Attributes["selected"] = "selected";
            }

            placeholderOption.InnerHtml.Append(Placeholder);
            selectTag.InnerHtml.Prepend(placeholderOption);
        }

        return selectTag;
    }

    private bool ShouldSelectPlaceholder()
    {
        var selectedValue = For.Model?.ToString();
        return string.IsNullOrWhiteSpace(selectedValue) || selectedValue == "0";
    }
}
