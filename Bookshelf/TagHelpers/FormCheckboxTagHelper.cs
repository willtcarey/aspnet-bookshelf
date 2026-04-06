using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-checkbox", Attributes = "asp-for")]
public class FormCheckboxTagHelper : FormTagHelperBase
{
    protected override string WrapperClasses => "fieldset gap-2";
    protected override string LabelClasses => "text-sm font-medium text-base-content cursor-pointer";

    public FormCheckboxTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    protected override TagBuilder GenerateInput()
    {
        var inputTag = Generator.GenerateCheckBox(
            ViewContext, For.ModelExplorer, For.Name, null, null);

        inputTag.AddCssClass("checkbox checkbox-primary");
        return inputTag;
    }

    protected override void RenderContent(
        TagHelperOutput output, TagBuilder labelTag, TagBuilder inputTag, TagBuilder validationTag)
    {
        var row = new TagBuilder("div");
        row.AddCssClass("flex items-center gap-3");
        row.InnerHtml.AppendHtml(inputTag);
        row.InnerHtml.AppendHtml(labelTag);

        output.Content.AppendHtml(row);
        output.Content.AppendHtml(validationTag);
    }
}
