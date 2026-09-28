using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

// Used both to schedule a new interview (ApplicationId is required) and to reschedule an existing one.
public class InterviewRequest
{
    public int ApplicationId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 30;

    [Required]
    public string Mode { get; set; } = "Online";

    [MaxLength(500)]
    public string? LocationOrLink { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateInterviewStatusRequest
{
    [Required]
    public string Status { get; set; } = "";
}

public class InterviewResponse
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = "";
    public string Company { get; set; } = "";
    public string ApplicantName { get; set; } = "";
    public string ApplicantEmail { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Mode { get; set; } = "Online";
    public string? LocationOrLink { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; }
}
