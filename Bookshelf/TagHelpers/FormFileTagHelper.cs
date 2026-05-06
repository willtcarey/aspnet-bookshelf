using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Bookshelf.TagHelpers;

/// <summary>
/// Renders a direct-upload file field. When a user selects a file, client-side
/// JavaScript uploads it to <see cref="UploadUrl"/> and stores the resulting
/// storage path in a hidden input bound to <c>asp-for</c>. The enclosing form
/// then submits the path reference, not the file.
/// </summary>
[HtmlTargetElement("form-file", Attributes = "asp-for")]
public class FormFileTagHelper : FormTagHelperBase
{
    [HtmlAttributeName("accept")]
    public string? Accept { get; set; }

    [HtmlAttributeName("hint")]
    public string? Hint { get; set; }

    /// <summary>
    /// The URL that the client-side JavaScript POSTs the selected file to.
    /// The endpoint must return JSON with a <c>path</c> property.
    /// </summary>
    [HtmlAttributeName("upload-url")]
    public string UploadUrl { get; set; } = "/images/create";

    public FormFileTagHelper(IHtmlGenerator generator, HtmlEncoder encoder)
        : base(generator, encoder) { }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var existingPath = For.Model as string;
        var hasExistingImage = !string.IsNullOrWhiteSpace(existingPath);

        // Wrapper — the data attribute is what direct-upload.js binds to.
        output.TagName = "fieldset";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", WrapperClasses);
        output.Attributes.SetAttribute("data-direct-upload", UploadUrl);

        // Label
        var labelTag = Generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, Label,
            new { @class = LabelClasses });
        output.Content.AppendHtml(labelTag);

        // Preview area — server-renders the current image when one exists;
        // replaced by JavaScript after a successful upload.
        output.Content.AppendHtml(BuildPreviewContainer(existingPath));

        // File input — used for selection only, not model-bound.
        output.Content.AppendHtml(BuildFileInput());

        // Hidden input — model-bound, carries the storage path on submit.
        output.Content.AppendHtml(BuildHiddenInput(existingPath));

        // Hint
        if (!string.IsNullOrWhiteSpace(Hint))
        {
            output.Content.AppendHtml(BuildHint(hasExistingImage));
        }

        // Error (initially hidden, shown by JS on upload failure)
        output.Content.AppendHtml(BuildErrorLabel());

        // Server-side validation
        var validationTag = Generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, null,
            ViewContext.ValidationMessageElement,
            new { @class = ValidationClasses });
        output.Content.AppendHtml(validationTag);
    }

    private static TagBuilder BuildPreviewContainer(string? existingPath)
    {
        var container = new TagBuilder("div");
        container.Attributes["data-upload-preview"] = string.Empty;

        if (string.IsNullOrWhiteSpace(existingPath))
        {
            container.AddCssClass("hidden");
            return container;
        }

        var card = new TagBuilder("div");
        card.AddCssClass("flex items-center gap-4 rounded-2xl bg-base-100 p-4 ring-1 ring-base-300/60");

        var img = new TagBuilder("img");
        img.Attributes["src"] = BuildImageUrl(existingPath, width: 64, height: 96);
        img.Attributes["alt"] = "Current image";
        img.AddCssClass("h-24 w-16 rounded-lg object-cover shadow");
        img.TagRenderMode = TagRenderMode.SelfClosing;

        var textBlock = new TagBuilder("div");
        textBlock.AddCssClass("text-sm text-base-content/70");

        var title = new TagBuilder("div");
        title.AddCssClass("font-semibold text-base-content");
        title.InnerHtml.Append("Current image");

        var subtitle = new TagBuilder("div");
        subtitle.InnerHtml.Append("Select a new file to replace it.");

        textBlock.InnerHtml.AppendHtml(title);
        textBlock.InnerHtml.AppendHtml(subtitle);

        card.InnerHtml.AppendHtml(img);
        card.InnerHtml.AppendHtml(textBlock);

        container.InnerHtml.AppendHtml(card);
        return container;
    }

    private TagBuilder BuildFileInput()
    {
        var input = new TagBuilder("input");
        input.Attributes["type"] = "file";
        input.Attributes["data-upload-input"] = string.Empty;
        input.AddCssClass("file-input file-input-bordered w-full");

        if (!string.IsNullOrWhiteSpace(Accept))
        {
            input.Attributes["accept"] = Accept;
        }

        return input;
    }

    private TagBuilder BuildHiddenInput(string? existingPath)
    {
        var input = new TagBuilder("input");
        input.Attributes["type"] = "hidden";
        input.Attributes["name"] = For.Name;
        input.Attributes["id"] = TagBuilder.CreateSanitizedId(For.Name, "_");
        input.Attributes["value"] = existingPath ?? string.Empty;
        input.Attributes["data-upload-path"] = string.Empty;
        return input;
    }

    private TagBuilder BuildHint(bool hidden)
    {
        var hint = new TagBuilder("label");
        hint.AddCssClass("label text-base-content/60");
        hint.Attributes["data-upload-hint"] = string.Empty;

        if (hidden)
        {
            hint.AddCssClass("hidden");
        }

        hint.InnerHtml.Append(Hint!);
        return hint;
    }

    private static TagBuilder BuildErrorLabel()
    {
        var error = new TagBuilder("label");
        error.AddCssClass("label text-error hidden");
        error.Attributes["data-upload-error"] = string.Empty;
        return error;
    }

    private static string BuildImageUrl(string path, int width, int height)
    {
        var encodedPath = string.Join(
            '/',
            path.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return $"/images/{encodedPath}?w={width}&h={height}";
    }

    // Required by the base class but unused — we override Process entirely.
    protected override TagBuilder GenerateInput() => new("input");
}
