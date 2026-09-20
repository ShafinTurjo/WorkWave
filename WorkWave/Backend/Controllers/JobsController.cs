using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public JobsController(ApplicationDbContext db)
    {
        _db = db;
    }

    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<JobResponse>>> GetAll()
    {
        // Reported / flagged / rejected / removed jobs are hidden from the public list.
        // (Admins still see them via GET api/jobs/flagged; owners via GET api/jobs/mine/{id}.)
        var jobs = await _db.Jobs
            .Where(j => !j.IsFlagged
                        && j.Status != "Flagged"
                        && j.Status != "Rejected"
                        && j.Status != "Removed")
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    
    [HttpGet("flagged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<JobResponse>>> GetFlaggedJobs()
    {
        var jobs = await _db.Jobs
            .Where(j => j.IsFlagged || j.Status == "Flagged")
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<JobResponse>> GetById(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        // Reported / flagged jobs must not be reachable by direct link either.
        if (!IsPublic(job) && !CanSeeHiddenJob(job))
        {
            return NotFound(new { message = $"Job {id} not found." });
        }

        return Ok(ToResponse(job));
    }

    [HttpGet("mine/{userId:int}")]
    [Authorize]
    public async Task<ActionResult<List<JobResponse>>> GetMine(int userId)
    {
        var denied = EnsureSelfOrAdmin(userId);
        if (denied is not null) return denied;

        var jobs = await _db.Jobs
            .Where(j => j.PostedByUserId == userId)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    [HttpPut("{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] UpdateJobStatusRequest request)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var isOwner = job.PostedByUserId == CurrentUserId;
        if (!isOwner && !IsAdmin) return Forbid();

        job.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewJob(int id, [FromQuery] bool approve, [FromQuery] string? rejectionReason = null)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        if (approve)
        {
            job.IsFlagged = false;
            job.Status = "Active";
            job.IsActive = true;
            job.FlagReason = null;
            job.RejectionReason = null;
        }
        else
        {
            job.IsFlagged = false;
            job.Status = "Rejected";
            job.IsActive = false;
            job.RejectionReason = rejectionReason ?? "Rejected by admin review.";
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = approve ? "Job approved successfully." : "Job rejected and marked as removed." });
    }

    
    [HttpPost("{id:int}/report")]
    [Authorize]
    public async Task<IActionResult> ReportJob(int id, [FromBody] ReportJobRequest request)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var report = new JobReport
        {
            JobId = id,
            ReportedByUserId = CurrentUserId,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        job.IsFlagged = true;
        job.FlagReason = $"Reported by user: {request.Reason}";

        _db.JobReports.Add(report);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Thank you for reporting. Our admin team will review this job." });
    }

    
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var isOwner = job.PostedByUserId == CurrentUserId;
        if (!isOwner && !IsAdmin) return Forbid();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<JobResponse>> Create(JobCreateRequest request)
    {
        var (isFraud, reason) = FraudDetector.EvaluateJob(request.Title, request.Description);

        var duplicate = await DuplicateJobDetector.CheckAsync(
            _db, CurrentUserId, request.Title, request.Company, request.Location, request.Description);

        // Same account, same job still open -> block it (usually a double click or an accidental re-post).
        if (duplicate.SameOwnerJobId is int existingJobId)
        {
            return Conflict(new
            {
                message = $"You already have an open job with the same title, company and location (job #{existingJobId}). " +
                          "Close or delete it first if you want to post it again."
            });
        }

        // Another account posted the same text -> let it through, but flag it so an admin reviews it.
        if (duplicate.OtherOwnerJobId is int otherJobId)
        {
            isFraud = true;
            reason = string.IsNullOrEmpty(reason)
                ? $"Possible duplicate of job #{otherJobId} posted by another account."
                : $"{reason}; possible duplicate of job #{otherJobId} posted by another account.";
        }

        var job = new Job
        {
            Title = request.Title,
            Company = request.Company,
            Location = request.Location,
            Category = request.Category,
            Salary = request.Salary,
            Description = request.Description,
            JobType = request.JobType,
            TagsCsv = request.Tags != null ? string.Join(",", request.Tags) : "",
            PostedByUserId = CurrentUserId,
            IsActive = true,

            IsFlagged = isFraud,
            FlagReason = isFraud ? reason : null,
            Status = isFraud ? "Flagged" : "Pending",
            RejectionReason = null
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToResponse(job));
    }

    // A job is visible to the public only while it is not reported/flagged/rejected/removed.
    private static bool IsPublic(Job job) =>
        !job.IsFlagged
        && job.Status != "Flagged"
        && job.Status != "Rejected"
        && job.Status != "Removed";

    // GetById is anonymous, so the caller may not be logged in: check before reading claims.
    private bool CanSeeHiddenJob(Job job)
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        return IsAdmin || job.PostedByUserId == CurrentUserId;
    }

    private static JobResponse ToResponse(Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        Company = job.Company,
        Location = job.Location,
        Category = job.Category,
        Salary = job.Salary,
        Description = job.Description,
        JobType = job.JobType,
        IsActive = job.IsActive,
        IsFlagged = job.IsFlagged,
        FlagReason = job.FlagReason,
        Status = job.Status,
        RejectionReason = job.RejectionReason,
        Tags = string.IsNullOrWhiteSpace(job.TagsCsv)
            ? Array.Empty<string>()
            : job.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        PostedAt = job.PostedAt
    };
}

public class ReportJobRequest
{
    public string Reason { get; set; } = string.Empty;
}