using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

public abstract class FormTagHelperBase : TagHelper
{
    protected IHtmlGenerator Generator { get; }
    protected HtmlEncoder Encoder { get; }

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("label")]
    public string? Label { get; set; }

    protected virtual string WrapperClasses => "fieldset w-full";
    protected virtual string LabelClasses => "fieldset-legend";
    protected virtual string ValidationClasses => "label text-error";

    protected FormTagHelperBase(IHtmlGenerator generator, HtmlEncoder encoder)
    {
        Generator = generator;
        Encoder = encoder;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.TagName = "fieldset";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", WrapperClasses);

        var labelTag = Generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, Label,
            new { @class = LabelClasses });

        var inputTag = GenerateInput();

        var validationTag = Generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, null,
            ViewContext.ValidationMessageElement,
            new { @class = ValidationClasses });

        RenderContent(output, labelTag, inputTag, validationTag);
    }

    protected abstract TagBuilder GenerateInput();

    protected virtual void RenderContent(
        TagHelperOutput output, TagBuilder labelTag, TagBuilder inputTag, TagBuilder validationTag)
    {
        ArgumentNullException.ThrowIfNull(output);

        _ = output.Content.AppendHtml(labelTag);
        _ = output.Content.AppendHtml(inputTag);
        _ = output.Content.AppendHtml(validationTag);
    }
}
