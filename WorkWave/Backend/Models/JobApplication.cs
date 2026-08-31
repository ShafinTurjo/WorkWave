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

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // "Pending" | "Accepted" | "Rejected"
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
}
