using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.TagHelpers;

internal static class TagBuilderExtensions
{
    public static void Prepend(this IHtmlContentBuilder builder, TagBuilder tag)
    {
        // IHtmlContentBuilder doesn't have a Prepend, so we work around it
        // by capturing existing content, clearing, adding the new item, then re-adding
        var existingContent = new HtmlContentBuilder();
        builder.MoveTo(existingContent);
        builder.AppendHtml(tag);
        existingContent.MoveTo(builder);
    }
}
