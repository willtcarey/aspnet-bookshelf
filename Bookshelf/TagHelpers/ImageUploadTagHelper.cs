using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("image-upload", Attributes = "path")]
public class ImageUploadTagHelper : TagHelper
{
    [HtmlAttributeName("path")]
    public string Path { get; set; } = string.Empty;

    [HtmlAttributeName("width")]
    public int? Width { get; set; }

    [HtmlAttributeName("height")]
    public int? Height { get; set; }

    [HtmlAttributeName("format")]
    public string? Format { get; set; }

    [HtmlAttributeName("alt")]
    public string? Alt { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrWhiteSpace(Path))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("src", BuildSource());

        if (!string.IsNullOrWhiteSpace(Alt))
        {
            output.Attributes.SetAttribute("alt", Alt);
        }
        else if (!output.Attributes.ContainsName("alt"))
        {
            output.Attributes.SetAttribute("alt", string.Empty);
        }
    }

    private string BuildSource()
    {
        var encodedPath = string.Join(
            '/',
            Path.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        var url = new StringBuilder($"/images/{encodedPath}");
        var queryParameters = new List<string>();

        if (Width is > 0)
        {
            queryParameters.Add($"w={Width.Value}");
        }

        if (Height is > 0)
        {
            queryParameters.Add($"h={Height.Value}");
        }

        if (!string.IsNullOrWhiteSpace(Format))
        {
            queryParameters.Add($"format={Uri.EscapeDataString(Format.Trim())}");
        }

        if (queryParameters.Count > 0)
        {
            url.Append('?');
            url.Append(string.Join('&', queryParameters));
        }

        return url.ToString();
    }
}
