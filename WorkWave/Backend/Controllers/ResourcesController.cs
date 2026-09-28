using Backend.Data;
using Backend.Dtos;
using Backend.Helpers;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ApiControllerBase
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
    private const string UploadsRelativePath = "uploads/resources";

    // Only harmless, non-executable, non-HTML types. (HTML/SVG/JS are deliberately excluded.)
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".zip", ".png", ".jpg", ".jpeg"
    };

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ResourcesController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Workers (and admins) can view all resources.
    [HttpGet]
    [Authorize(Roles = "Worker,Admin")]
    public async Task<ActionResult<List<ResourceResponse>>> GetAll()
    {
        var items = await _db.Resources
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(ToResponse).ToList());
    }

    // Only admins can add resources.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSizeBytes + 1024 * 1024)]
    public async Task<ActionResult<ResourceResponse>> Create([FromForm] CreateResourceRequest request)
    {
        var hasLink = !string.IsNullOrWhiteSpace(request.YoutubeUrl);
        var hasFile = request.File is not null && request.File.Length > 0;

        if (hasLink == hasFile)
        {
            return BadRequest(new { message = "Provide either a YouTube link or a file (not both)." });
        }

        var resource = new Resource
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedByUserId = CurrentUserId
        };

        if (hasLink)
        {
            var videoId = YouTubeHelper.ExtractVideoId(request.YoutubeUrl);
            if (videoId is null)
            {
                return BadRequest(new { message = "That doesn't look like a valid YouTube video link." });
            }

            resource.Type = "Video";
            resource.YoutubeUrl = request.YoutubeUrl!.Trim();
            resource.YoutubeVideoId = videoId;
        }
        else
        {
            var file = request.File!;

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(new { message = "File must be 20 MB or smaller." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = $"This file type isn't allowed. Allowed: {string.Join(", ", AllowedExtensions.OrderBy(x => x))}"
                });
            }

            var uploadsDir = GetUploadsDir();
            Directory.CreateDirectory(uploadsDir);

            var storedFileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsDir, storedFileName);
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            resource.Type = "File";
            resource.StoredFileName = storedFileName;
            resource.OriginalFileName = Path.GetFileName(file.FileName);
            resource.FileSize = file.Length;
        }

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(resource));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource is null) return NotFound(new { message = $"Resource {id} not found." });

        if (!string.IsNullOrEmpty(resource.StoredFileName))
        {
            var path = Path.Combine(GetUploadsDir(), resource.StoredFileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private string GetUploadsDir()
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }
        return Path.Combine(webRoot, "uploads", "resources");
    }

    private ResourceResponse ToResponse(Resource r)
    {
        var response = new ResourceResponse
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Type = r.Type,
            CreatedAt = r.CreatedAt
        };

        if (r.Type == "Video" && !string.IsNullOrEmpty(r.YoutubeVideoId))
        {
            response.WatchUrl = $"https://www.youtube.com/watch?v={r.YoutubeVideoId}";
            response.EmbedUrl = $"https://www.youtube-nocookie.com/embed/{r.YoutubeVideoId}";
            response.ThumbnailUrl = $"https://img.youtube.com/vi/{r.YoutubeVideoId}/hqdefault.jpg";
        }
        else if (!string.IsNullOrEmpty(r.StoredFileName))
        {
            response.FileUrl = $"{Request.Scheme}://{Request.Host}/{UploadsRelativePath}/{r.StoredFileName}";
            response.OriginalFileName = r.OriginalFileName;
            response.FileSize = r.FileSize;
        }

        return response;
    }
}
