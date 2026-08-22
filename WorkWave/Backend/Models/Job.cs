using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Job
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = "";

    [Required, MaxLength(150)]
    public string Company { get; set; } = "";

    [MaxLength(150)]
    public string Location { get; set; } = "";

    [MaxLength(100)]
    public string Category { get; set; } = "";

    [MaxLength(100)]
    public string Salary { get; set; } = "";

    public string Description { get; set; } = "";

    [MaxLength(50)]
    public string JobType { get; set; } = "";

    // Stored as a comma-separated string; exposed as string[] via the API DTO.
    public string TagsCsv { get; set; } = "";

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Who posted this job.
    public int PostedByUserId { get; set; }
    public User? PostedByUser { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
