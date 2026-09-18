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

    // GET api/jobs (শুধুমাত্র Active এবং Flagged না হওয়া চাকরিগুলো সাধারণ ইউজারদের দেখাবে)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<JobResponse>>> GetAll()
    {
        var jobs = await _db.Jobs
            .Where(j => j.IsActive && !j.IsFlagged && j.Status == "Active")
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    // GET api/jobs/flagged (শুধুমাত্র Admin ফ্ল্যাগ হওয়া বা ফ্ল্যাগড জবেগুলো রিভিউয়ের জন্য দেখতে পাবে)
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

    // GET api/jobs/5 — publicly viewable
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<JobResponse>> GetById(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        return Ok(ToResponse(job));
    }

    // GET api/jobs/mine/5 (একক নিয়োগকর্তার সব Active ও Closed চাকরিগুলো দেখাবে)
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

    // PUT api/jobs/5/status (স্ট্যাটাস Active/Closed করার জন্য)
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

    // PUT api/jobs/5/review (Admin ফ্ল্যাগ করা জব Unflag/Approve বা Ban করতে পারবে)
    [HttpPut("{id:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewJob(int id, [FromQuery] bool approve)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        if (approve)
        {
            job.IsFlagged = false;
            job.Status = "Active";
            job.FlagReason = null;
        }
        else
        {
            job.IsFlagged = true;
            job.Status = "Removed";
            job.IsActive = false;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = approve ? "Job approved successfully." : "Job removed and marked as fraud." });
    }

    // POST api/jobs/5/report (Worker/User কোনো জব রিপোর্ট করার জন্য)
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

        // ইউজারের রিপোর্টের ভিত্তিতে জবটিকে ফ্ল্যাগড মার্ক করা
        job.IsFlagged = true;
        job.FlagReason = $"Reported by user: {request.Reason}";

        _db.JobReports.Add(report);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Thank you for reporting. Our admin team will review this job." });
    }

    // DELETE api/jobs/5
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

    // POST api/jobs (ডিটেকটর সার্ভিস দিয়ে ফ্রড ফিল্টারসহ জব পোস্ট তৈরি)
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<JobResponse>> Create(JobCreateRequest request)
    {
        // FraudDetector সার্ভিস চালিয়ে টেক্সট চেক
        var (isFraud, reason) = FraudDetector.EvaluateJob(request.Title, request.Description);

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
            Status = isFraud ? "Flagged" : "Active"
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToResponse(job));
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