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

        return result.IsSuccess
            ? Ok(new { path = result.Path })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{*path}")]
    [HttpHead("{*path}")]
    public async Task<IActionResult> Get(
        string? path,
        [FromQuery(Name = "w")] int? width,
        [FromQuery(Name = "h")] int? height,
        [FromQuery] string format = "webp")
    {
        var result = await _imageUpload.GetAsync(path, width, height, format);

        return result switch
        {
            ImageStreamResult stream => WithCacheHeaders(File(stream.Stream, stream.ContentType)),
            ImageFileResult file => WithCacheHeaders(PhysicalFile(file.FilePath, file.ContentType)),
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
