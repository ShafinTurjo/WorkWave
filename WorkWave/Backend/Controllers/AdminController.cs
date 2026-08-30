using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Every admin endpoint requires ?adminUserId=<id> for a user whose Role is "Admin".
    private async Task<bool> IsAdminAsync(int adminUserId)
    {
        return await _db.Users.AnyAsync(u => u.Id == adminUserId && u.Role == "Admin");
    }

    // GET api/admin/users?adminUserId=1
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var users = await _db.Users
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // DELETE api/admin/users/5?adminUserId=1
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();
        if (id == adminUserId) return BadRequest(new { message = "You cannot delete your own admin account." });

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} not found." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET api/admin/jobs?adminUserId=1
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var jobs = await _db.Jobs
            .OrderByDescending(j => j.PostedAt)
            .Select(j => new
            {
                j.Id,
                j.Title,
                j.Company,
                j.Location,
                j.JobType,
                j.PostedAt,
                j.PostedByUserId,
                PostedByName = j.PostedByUser != null ? j.PostedByUser.FullName : ""
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // DELETE api/admin/jobs/5?adminUserId=1
    [HttpDelete("jobs/{id:int}")]
    public async Task<IActionResult> DeleteJob(int id, [FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET api/admin/applications?adminUserId=1
    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications([FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var applications = await _db.JobApplications
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new
            {
                a.Id,
                a.JobId,
                JobTitle = a.Job != null ? a.Job.Title : "",
                a.ApplicantName,
                a.ApplicantEmail,
                a.AppliedAt
            })
            .ToListAsync();

        return Ok(applications);
    }
}