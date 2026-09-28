using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

// Used both to schedule a new interview (ApplicationId is required) and to reschedule an existing one.
// Property names match exactly what the Blazor frontend sends (camelCase JSON).
public class InterviewRequest
{
    public int ApplicationId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 30;

    // "Online" | "InPerson" | "Phone"
    [Required]
    public string Mode { get; set; } = "Online";

    // Meeting link (Online), address (InPerson) or phone number (Phone).
    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Browser UTC offset in minutes (Dhaka = 360); used only to format times in emails.
    public int? UtcOffsetMinutes { get; set; }
}

// PUT api/interviews/{id}/respond  — the applicant confirms or declines.
public class InterviewRespondRequest
{
    public bool Confirm { get; set; }
    public int? UtcOffsetMinutes { get; set; }
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
    public string Location { get; set; } = "";
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; }
}
