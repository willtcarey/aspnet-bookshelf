using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-checkbox", Attributes = "asp-for")]
public class FormCheckboxTagHelper : FormTagHelperBase
{
    public FormCheckboxTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    protected override TagBuilder GenerateInput()
    {
        return Generator.GenerateCheckBox(
            ViewContext, For.ModelExplorer, For.Name, null, null);
    }

    protected override void RenderContent(
        TagHelperOutput output, TagBuilder labelTag, TagBuilder inputTag, TagBuilder validationTag)
    {
        // For checkboxes: input first, then label, then validation
        output.Content.AppendHtml(inputTag);
        output.Content.AppendHtml(labelTag);
        output.Content.AppendHtml(validationTag);
    }
}
