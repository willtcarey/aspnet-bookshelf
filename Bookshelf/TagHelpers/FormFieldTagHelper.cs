using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("form-field", Attributes = "asp-for")]
public class FormFieldTagHelper : TagHelper
{
    private readonly IHtmlGenerator _generator;
    private readonly HtmlEncoder _encoder;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("label")]
    public string? Label { get; set; }

    [HtmlAttributeName("asp-items")]
    public SelectList? Items { get; set; }

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    [HtmlAttributeName("type")]
    public string? InputType { get; set; }

    public FormFieldTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
    {
        _generator = generator;
        _encoder = encoder;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        // Generate label
        var labelTag = _generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, Label, null);

        // Generate input or select
        TagBuilder inputTag;
        if (Items != null)
        {
            inputTag = _generator.GenerateSelect(
                ViewContext, For.ModelExplorer, null, For.Name, Items,
                allowMultiple: false, htmlAttributes: null);

            if (!string.IsNullOrEmpty(Placeholder))
            {
                var placeholderOption = new TagBuilder("option");
                placeholderOption.Attributes["value"] = "";
                placeholderOption.InnerHtml.Append(Placeholder);
                inputTag.InnerHtml.Prepend(placeholderOption);
            }
        }
        else
        {
            inputTag = _generator.GenerateTextBox(
                ViewContext, For.ModelExplorer, For.Name, For.Model,
                null, new { type = InputType ?? "text" });
        }

        // Generate validation message
        var validationTag = _generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, null,
            ViewContext.ValidationMessageElement, null);

        // Write them all out
        output.Content.AppendHtml(labelTag);
        output.Content.AppendHtml(inputTag);
        output.Content.AppendHtml(validationTag);
    }
}

internal static class TagBuilderExtensions
{
    public static void Prepend(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, TagBuilder tag)
    {
        // IHtmlContentBuilder doesn't have a Prepend, so we work around it
        // by capturing existing content, clearing, adding the new item, then re-adding
        var existingContent = new Microsoft.AspNetCore.Html.HtmlContentBuilder();
        builder.MoveTo(existingContent);
        builder.AppendHtml(tag);
        existingContent.MoveTo(builder);
    }
}
