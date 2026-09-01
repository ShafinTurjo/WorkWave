using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class JobApplication
{
    public int Id { get; set; }

    public int JobId { get; set; }
    public Job? Job { get; set; }

    public int ApplicantUserId { get; set; }
    public User? ApplicantUser { get; set; }

    [MaxLength(150)]
    public string ApplicantName { get; set; } = "";

    [MaxLength(150)]
    public string ApplicantEmail { get; set; } = "";

    public string CoverLetter { get; set; } = "";

    // Stored on disk under wwwroot/uploads/resumes as "{Guid}.pdf".
    [MaxLength(260)]
    public string? ResumeFileName { get; set; }

    // Original file name chosen by the applicant, kept for display/download purposes.
    [MaxLength(260)]
    public string? ResumeOriginalFileName { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // "Pending" | "Accepted" | "Rejected"
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
}
