using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Backend.Dtos;

public class ApplyRequest
{
    [Required]
    public int JobId { get; set; }

    
    [Required]
    public int ApplicantUserId { get; set; }

    [Required, MaxLength(150)]
    public string ApplicantName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string ApplicantEmail { get; set; } = "";

    public string CoverLetter { get; set; } = "";

    
    public IFormFile? Resume { get; set; }
}

public class ApplicationResponse
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = "";
    public string ApplicantName { get; set; } = "";
    public string ApplicantEmail { get; set; } = "";
    public DateTime AppliedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ResumeOriginalFileName { get; set; }
    public string? ResumeUrl { get; set; }

    
    public int MatchScore { get; set; }
}

public class UpdateApplicationStatusRequest
{
    
    [Required]
    public int RequestingUserId { get; set; }

    [Required]
    public string Status { get; set; } = "";
}