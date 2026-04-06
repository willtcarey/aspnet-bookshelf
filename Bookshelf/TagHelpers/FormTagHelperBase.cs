using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

public abstract class FormTagHelperBase : TagHelper
{
    protected readonly IHtmlGenerator Generator;
    protected readonly HtmlEncoder Encoder;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("label")]
    public string? Label { get; set; }

    protected FormTagHelperBase(IHtmlGenerator generator, HtmlEncoder encoder)
    {
        Generator = generator;
        Encoder = encoder;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        var labelTag = Generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, Label, null);

        var inputTag = GenerateInput();

        var validationTag = Generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, null,
            ViewContext.ValidationMessageElement, null);

        RenderContent(output, labelTag, inputTag, validationTag);
    }

    protected abstract TagBuilder GenerateInput();

    protected virtual void RenderContent(
        TagHelperOutput output, TagBuilder labelTag, TagBuilder inputTag, TagBuilder validationTag)
    {
        output.Content.AppendHtml(labelTag);
        output.Content.AppendHtml(inputTag);
        output.Content.AppendHtml(validationTag);
    }
}
