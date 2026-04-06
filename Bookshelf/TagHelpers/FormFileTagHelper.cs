using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-file", Attributes = "asp-for")]
public class FormFileTagHelper : FormTagHelperBase
{
    [HtmlAttributeName("accept")]
    public string? Accept { get; set; }

    [HtmlAttributeName("hint")]
    public string? Hint { get; set; }

    public FormFileTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    protected override TagBuilder GenerateInput()
    {
        var inputTag = new TagBuilder("input");
        inputTag.Attributes["type"] = "file";
        inputTag.Attributes["id"] = TagBuilder.CreateSanitizedId(For.Name, "_");
        inputTag.Attributes["name"] = For.Name;
        inputTag.AddCssClass("file-input file-input-bordered w-full");

        if (!string.IsNullOrWhiteSpace(Accept))
        {
            inputTag.Attributes["accept"] = Accept;
        }

        return inputTag;
    }

    protected override void RenderContent(
        TagHelperOutput output, TagBuilder labelTag, TagBuilder inputTag, TagBuilder validationTag)
    {
        output.Content.AppendHtml(labelTag);
        output.Content.AppendHtml(inputTag);

        if (!string.IsNullOrWhiteSpace(Hint))
        {
            var hintTag = new TagBuilder("label");
            hintTag.AddCssClass("label text-base-content/60");
            hintTag.InnerHtml.Append(Hint);
            output.Content.AppendHtml(hintTag);
        }

        output.Content.AppendHtml(validationTag);
    }
}
