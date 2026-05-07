using Bookshelf.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Bookshelf.Controllers;

[Route("images")]
public class ImagesController : Controller
{
    private readonly ImageUpload _imageUpload;

    public ImagesController(ImageUpload imageUpload)
    {
        _imageUpload = imageUpload;
    }

    [HttpPost("create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? file)
    {
        var result = await _imageUpload.SaveAsync(file);

        return result.IsSuccess && result.Path is not null
            ? Ok(new { path = result.Path, previewUrl = _imageUpload.BuildUrl(result.Path, 64, 96) })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{key}")]
    [HttpHead("{key}")]
    public async Task<IActionResult> Get(
        string? key,
        [FromQuery(Name = "w")] int? width,
        [FromQuery(Name = "h")] int? height,
        [FromQuery] string format = "webp")
    {
        var result = await _imageUpload.GetAsync(key, width, height, format);

        return result switch
        {
            ImageStreamResult stream => WithCacheHeaders(File(stream.Stream, stream.ContentType)),
            ImageErrorResult error => BadRequest(error.Message),
            _ => NotFound()
        };
    }

    private IActionResult WithCacheHeaders(IActionResult inner)
    {
        Response.Headers[HeaderNames.CacheControl] = "public,max-age=2592000,immutable";
        return inner;
    }
}
