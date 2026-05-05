using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("sortable-column")]
public class SortableColumnTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("name")]
    public string Name { get; set; } = string.Empty;

    [HtmlAttributeName("current-sort")]
    public string? CurrentSort { get; set; }

    [HtmlAttributeName("current-direction")]
    public string? CurrentDirection { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.TagName = "th";
        output.TagMode = TagMode.StartTagAndEndTag;

        var isActive = string.Equals(Name, CurrentSort, StringComparison.OrdinalIgnoreCase);
        var nextDirection = isActive && string.Equals(CurrentDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";

        // Build query string preserving existing params
        var query = ViewContext.HttpContext.Request.Query;
        var queryParams = new List<string>
        {
            $"sort={Uri.EscapeDataString(Name)}",
            $"dir={Uri.EscapeDataString(nextDirection)}"
        };

        foreach (var param in query)
        {
            var key = param.Key.ToUpperInvariant();
            if (key != "SORT" && key != "DIR")
            {
                foreach (var value in param.Value)
                {
                    queryParams.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value ?? "")}");
                }
            }
        }

        var url = $"?{string.Join("&", queryParams)}";

        var arrow = "";
        if (isActive)
        {
            arrow = string.Equals(CurrentDirection, "asc", StringComparison.OrdinalIgnoreCase)
                ? " ▲"
                : " ▼";
        }

        var childContent = output.GetChildContentAsync().Result;
        var label = childContent.IsEmptyOrWhiteSpace ? Name : childContent.GetContent();

        output.Content.SetHtmlContent(
            $"<a href=\"{url}\" class=\"link link-hover inline-flex items-center gap-1\">{label}{arrow}</a>");
    }
}
