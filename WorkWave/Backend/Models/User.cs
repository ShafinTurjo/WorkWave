using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(150)]
    public string Email { get; set; } = "";

    // Store only a hash, never the raw password.
    [Required]
    public string PasswordHash { get; set; } = "";
    // "Admin", "Employer", or "Worker"
    [Required, MaxLength(20)]
    public string Role { get; set; } = "Worker";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> PostedJobs { get; set; } = new List<Job>();
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
