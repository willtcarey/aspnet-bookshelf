using Bookshelf.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Bookshelf.TagHelpers;

[HtmlTargetElement("image-upload", Attributes = "path")]
public class ImageUploadTagHelper : TagHelper
{
    private readonly ImageStorage _imageStorage;

    public ImageUploadTagHelper(ImageStorage imageStorage)
    {
        _imageStorage = imageStorage;
    }

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

        var source = _imageStorage.BuildUrl(Path, Width, Height, Format);
        if (source is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("src", source);

        if (!string.IsNullOrWhiteSpace(Alt))
        {
            output.Attributes.SetAttribute("alt", Alt);
        }
        else if (!output.Attributes.ContainsName("alt"))
        {
            output.Attributes.SetAttribute("alt", string.Empty);
        }
    }
}
