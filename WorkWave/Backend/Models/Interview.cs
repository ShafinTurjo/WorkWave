using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// An interview an employer (or admin) schedules for a job application.
/// </summary>
public class Interview
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }

    // Always stored in UTC.
    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 30;

    // "Online" | "Onsite" | "Phone"
    [Required, MaxLength(10)]
    public string Mode { get; set; } = "Online";

    // Meeting link (Online), address (Onsite) or phone number (Phone).
    [MaxLength(500)]
    public string? LocationOrLink { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // "Scheduled" | "Completed" | "Cancelled"
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Scheduled";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedByUserId { get; set; }
}
