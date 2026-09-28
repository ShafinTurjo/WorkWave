using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class CreateResourceRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Provide EITHER a YouTube link OR a file — not both.
    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    public IFormFile? File { get; set; }
}

public class ResourceResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // Video
    public string? WatchUrl { get; set; }
    public string? EmbedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    // File
    public string? FileUrl { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSize { get; set; }
}
