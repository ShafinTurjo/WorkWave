using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class ApplyRequest
{
    [Required]
    public int JobId { get; set; }

    // Until real auth/JWT is added, the applicant's user id is sent explicitly.
    [Required]
    public int ApplicantUserId { get; set; }

    [Required, MaxLength(150)]
    public string ApplicantName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string ApplicantEmail { get; set; } = "";

    public string CoverLetter { get; set; } = "";
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
}

public class UpdateApplicationStatusRequest
{
    // Until real auth/JWT is added, the requester's user id is sent explicitly
    // so the API can confirm they own the job (or are an Admin).
    [Required]
    public int RequestingUserId { get; set; }

    [Required]
    public string Status { get; set; } = "";
}
