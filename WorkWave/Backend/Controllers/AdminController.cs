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

    private async Task<bool> IsAdminAsync(int adminUserId)
    {
        return await _db.Users.AnyAsync(u => u.Id == adminUserId && u.Role == "Admin");
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // গত ৭ দিনের প্রতিদিনের সাইনআপ সংখ্যা
        var dailySignups = new int[7];
        for (int i = 0; i < 7; i++)
        {
            var day = DateTime.UtcNow.Date.AddDays(-6 + i);
            dailySignups[i] = await _db.Users.CountAsync(u => u.CreatedAt.Date == day);
        }

        // ডায়নামিক রিসেন্ট অ্যাক্টিভিটি ডাটা তৈরি
        var recentActivityList = new List<object>();

        var latestJob = await _db.Jobs
            .Include(j => j.PostedByUser)
            .OrderByDescending(j => j.PostedAt)
            .FirstOrDefaultAsync();

        if (latestJob != null)
        {
            recentActivityList.Add(new
            {
                Icon = "bi-briefcase text-primary",
                Text = $"{latestJob.PostedByUser?.FullName ?? "Someone"} posted",
                Highlight = latestJob.Title
            });
        }

        var latestApp = await _db.JobApplications
            .Include(a => a.Job)
            .OrderByDescending(a => a.AppliedAt)
            .FirstOrDefaultAsync();

        if (latestApp != null)
        {
            recentActivityList.Add(new
            {
                Icon = "bi-file-earmark-text text-success",
                Text = $"{latestApp.ApplicantName} applied for",
                Highlight = latestApp.Job?.Title ?? "a Job"
            });
        }

        var latestUser = await _db.Users
            .Where(u => u.Role != "Admin")
            .OrderByDescending(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestUser != null)
        {
            recentActivityList.Add(new
            {
                Icon = "bi-person-plus text-warning",
                Text = $"{latestUser.FullName} joined as",
                Highlight = latestUser.Role
            });
        }

        var stats = new
        {
            TotalUsers = await _db.Users.CountAsync(),
            NewUsersThisWeek = await _db.Users.CountAsync(u => u.CreatedAt >= sevenDaysAgo),
            TotalJobs = await _db.Jobs.CountAsync(),
            ActiveJobs = await _db.Jobs.CountAsync(j => j.IsActive),
            TotalApplications = await _db.JobApplications.CountAsync(),
            PendingApplications = await _db.JobApplications.CountAsync(a => a.Status == "Pending"),
            FlaggedContent = 0,
            DailySignups = dailySignups,
            RecentActivities = recentActivityList
        };

        return Ok(stats);
    }

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
                Status = "Active",
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string newRole, [FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();
        if (id == adminUserId) return BadRequest(new { message = "You cannot change your own admin role." });

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} not found." });

        user.Role = newRole;
        await _db.SaveChangesAsync();

        return NoContent();
    }

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
                j.IsActive,
                j.PostedAt,
                j.PostedByUserId,
                PostedByName = j.PostedByUser != null ? j.PostedByUser.FullName : ""
            })
            .ToListAsync();

        return Ok(jobs);
    }

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
                a.Status,
                a.AppliedAt
            })
            .ToListAsync();

        return Ok(applications);
    }

    [HttpDelete("applications/{id:int}")]
    public async Task<IActionResult> DeleteApplication(int id, [FromQuery] int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return Forbid();

        var app = await _db.JobApplications.FindAsync(id);
        if (app is null) return NotFound(new { message = $"Application {id} not found." });

        _db.JobApplications.Remove(app);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}