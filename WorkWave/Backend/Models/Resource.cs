using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// A learning resource added by an Admin: either a YouTube video link or an uploaded file.
/// </summary>
public class Resource
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    // Short text explaining what the resource is about.
    [MaxLength(1000)]
    public string? Description { get; set; }

    // "Video" or "File"
    [Required, MaxLength(10)]
    public string Type { get; set; } = "Video";

    // Video resources
    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    [MaxLength(20)]
    public string? YoutubeVideoId { get; set; }

    // File resources
    [MaxLength(100)]
    public string? StoredFileName { get; set; }

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    public long? FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedByUserId { get; set; }
}
