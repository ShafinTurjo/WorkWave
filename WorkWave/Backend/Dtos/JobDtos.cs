using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class JobCreateRequest
{
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

    public string[] Tags { get; set; } = Array.Empty<string>();

    [Required]
    public int PostedByUserId { get; set; }
}

public class JobResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Company { get; set; } = "";
    public string Location { get; set; } = "";
    public string Category { get; set; } = "";
    public string Salary { get; set; } = "";
    public string Description { get; set; } = "";
    public string JobType { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime PostedAt { get; set; }
}

public class EmployerDashboardStatsDto
{
    public int TotalJobs { get; set; }
    public int TotalApplicants { get; set; }
    public int ActiveJobs { get; set; }
    public int ClosedJobs { get; set; }
}

public class UpdateJobStatusDto
{
    public string Status { get; set; } = string.Empty;
}